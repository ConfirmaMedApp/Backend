using System.Globalization;
using System.Text.Json;
using Backend.DTOs.Appointments.Responses;
using Backend.DTOs.Emails;
using Backend.DTOs.Payments.Responses;
using Backend.DTOs.Wompi.Responses;
using Backend.Entities.Payments;
using Backend.Entities.Wompi;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Helpers;
using Backend.Repositories.Appointments;
using Backend.Repositories.Payments;
using Backend.Services.Bird;
using Backend.Services.Offices;
using Backend.Services.Patients;
using Backend.Services.Trm;
using Backend.Services.Users;
using Backend.Services.Wompi;
using Microsoft.Extensions.Options;

namespace Backend.Services.Payments;

public class AppointmentPaymentService(
    IAppointmentRepository appointmentRepository,
    IAppointmentPaymentRepository paymentRepository,
    IWompiService wompiService,
    ITrmService trmService,
    IPatientService patientService,
    IUserService userService,
    IOfficeService officeService,
    IBirdEmailService birdEmailService,
    IOptions<WompiSettings> wompiOptions,
    ILogger<AppointmentPaymentService> logger) : IAppointmentPaymentService
{
    private readonly WompiSettings _wompi = wompiOptions.Value;

    public async Task<PaymentLinkResultDto> GeneratePaymentLinkAsync(int appointmentId, bool sendEmail, CancellationToken ct = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId)
            ?? throw new NotFoundException("Cita no encontrada");

        var info = await paymentRepository.GetAppointmentPaymentInfoAsync(appointmentId)
            ?? throw new NotFoundException("Cita no encontrada");

        var priceUsd = info.PriceUsd
            ?? throw new BadRequestException(
                "La cita no tiene precio configurado. Define el precio del doctor para esa especialidad primero.");

        if (priceUsd <= 0)
            throw new BadRequestException("El precio de la cita debe ser mayor a 0");

        var trm = await trmService.GetUsdToCopAsync(ct);

        // USD -> COP (pesos enteros) -> centavos que exige Wompi.
        var pesos = Math.Round(priceUsd * trm, 0, MidpointRounding.AwayFromZero);
        var amountInCents = (long)pesos * 100L;

        var name = $"Consulta {appointment.SpecialityName}";
        var description = $"Cita {appointment.DateAppointment} {appointment.StartHour} - Dr. {appointment.DoctorName} {appointment.DoctorLastname}".Trim();

        var expiresAt = DateTimeOffset.UtcNow.AddHours(Math.Max(1, _wompi.ExpirationHours));
        var redirectUrl = string.IsNullOrWhiteSpace(_wompi.RedirectUrl) ? null : _wompi.RedirectUrl;

        var link = await wompiService.CreatePaymentLinkAsync(name, description, amountInCents, expiresAt, redirectUrl, ct);
        var checkoutUrl = wompiService.BuildCheckoutUrl(link.Id);

        await paymentRepository.CreateAsync(new AppointmentPayment
        {
            AppointmentId = appointmentId,
            WompiPaymentLinkId = link.Id,
            CheckoutUrl = checkoutUrl,
            AmountUsd = priceUsd,
            TrmUsed = trm,
            AmountInCents = amountInCents,
            Currency = "COP",
            Status = "PENDING",
            ExpiresAt = expiresAt.UtcDateTime
        });

        var emailSent = false;
        if (sendEmail)
            emailSent = await TrySendPaymentEmailAsync(appointment, checkoutUrl, priceUsd, pesos);

        return new PaymentLinkResultDto
        {
            AppointmentId = appointmentId,
            PaymentLinkId = link.Id,
            CheckoutUrl = checkoutUrl,
            AmountUsd = priceUsd,
            Trm = trm,
            AmountCop = pesos,
            AmountInCents = amountInCents,
            Currency = "COP",
            ExpiresAt = expiresAt,
            EmailSent = emailSent
        };
    }

    public async Task<CheckoutConfigResultDto> GenerateCheckoutConfigAsync(int appointmentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_wompi.PublicKey))
            throw new BadRequestException("Wompi no está configurado para el widget (falta la llave pública)");

        var info = await paymentRepository.GetAppointmentPaymentInfoAsync(appointmentId)
            ?? throw new NotFoundException("Cita no encontrada");

        var priceUsd = info.PriceUsd
            ?? throw new BadRequestException(
                "La cita no tiene precio configurado. Define el precio del doctor para esa especialidad primero.");

        if (priceUsd <= 0)
            throw new BadRequestException("El precio de la cita debe ser mayor a 0");

        var trm = await trmService.GetUsdToCopAsync(ct);

        // USD -> COP (pesos enteros) -> centavos que exige Wompi.
        var pesos = Math.Round(priceUsd * trm, 0, MidpointRounding.AwayFromZero);
        var amountInCents = (long)pesos * 100L;

        const string currency = "COP";
        var reference = $"CITA-{appointmentId}-{Guid.NewGuid():N}";
        var signature = wompiService.BuildIntegritySignature(reference, amountInCents, currency);

        // Persistimos el pago (sin link, con referencia) para correlacionar el webhook.
        await paymentRepository.CreateAsync(new AppointmentPayment
        {
            AppointmentId = appointmentId,
            WompiPaymentLinkId = null,
            CheckoutUrl = null,
            AmountUsd = priceUsd,
            TrmUsed = trm,
            AmountInCents = amountInCents,
            Currency = currency,
            WompiReference = reference,
            Status = "PENDING",
            ExpiresAt = null
        });

        return new CheckoutConfigResultDto
        {
            AppointmentId = appointmentId,
            PublicKey = _wompi.PublicKey,
            Currency = currency,
            AmountInCents = amountInCents,
            Reference = reference,
            SignatureIntegrity = signature,
            RedirectUrl = string.IsNullOrWhiteSpace(_wompi.RedirectUrl) ? null : _wompi.RedirectUrl,
            AmountUsd = priceUsd,
            Trm = trm,
            AmountCop = pesos
        };
    }

    public async Task<AppointmentPricingResponseDto> GetPricingAsync(int appointmentId, CancellationToken ct = default)
    {
        var info = await paymentRepository.GetAppointmentPaymentInfoAsync(appointmentId)
            ?? throw new NotFoundException("Cita no encontrada");

        decimal trm = 0m;
        decimal priceCop = 0m;

        if (info.PriceUsd is > 0)
        {
            trm = await trmService.GetUsdToCopAsync(ct);
            priceCop = Math.Round(info.PriceUsd.Value * trm, 0, MidpointRounding.AwayFromZero);
        }

        return new AppointmentPricingResponseDto
        {
            AppointmentId = appointmentId,
            PriceUsd = info.PriceUsd,
            Trm = trm,
            PriceCop = priceCop,
            Currency = "COP",
            PaymentStatus = info.PaymentStatus
        };
    }

    public async Task<AppointmentPaymentStatusResponseDto> GetPaymentStatusAsync(int appointmentId)
    {
        var info = await paymentRepository.GetAppointmentPaymentInfoAsync(appointmentId)
            ?? throw new NotFoundException("Cita no encontrada");

        var payment = await paymentRepository.GetLatestByAppointmentIdAsync(appointmentId);

        return new AppointmentPaymentStatusResponseDto
        {
            AppointmentId = appointmentId,
            PaymentStatus = info.PaymentStatus,
            PaymentLinkId = payment?.WompiPaymentLinkId,
            CheckoutUrl = payment?.CheckoutUrl,
            TransactionStatus = payment?.Status,
            AmountUsd = payment?.AmountUsd,
            AmountCop = payment is null ? null : Math.Round(payment.AmountInCents / 100m, 0),
            Currency = payment?.Currency ?? "COP",
            ExpiresAt = payment?.ExpiresAt,
            PaidAt = payment?.PaidAt
        };
    }

    public async Task<bool> HandleWebhookAsync(string rawBody, string? checksumHeader)
    {
        if (!wompiService.VerifyEventSignature(rawBody, checksumHeader))
        {
            logger.LogWarning("Webhook de Wompi con firma inválida; se ignora");
            return false;
        }

        WompiEventDto? evt;
        try
        {
            evt = JsonSerializer.Deserialize<WompiEventDto>(rawBody);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo deserializar el webhook de Wompi");
            return false;
        }

        if (evt is null || evt.Event != "transaction.updated")
            return true; // recibido pero sin acción

        var tx = evt.Data.Transaction;
        if (tx is null)
            return true;

        var status = string.IsNullOrWhiteSpace(tx.Status) ? "UNKNOWN" : tx.Status.ToUpperInvariant();
        var approved = status == "APPROVED";
        DateTime? paidAt = approved ? DateTime.UtcNow : null;

        int? appointmentId;
        string correlation;

        if (!string.IsNullOrWhiteSpace(tx.PaymentLinkId))
        {
            // Flujo de ENLACES de pago.
            correlation = $"payment_link_id {tx.PaymentLinkId}";
            appointmentId = await paymentRepository.ApplyTransactionUpdateByLinkIdAsync(
                tx.PaymentLinkId, status, tx.Id, tx.Reference, paidAt, markAppointmentPaid: approved);
        }
        else if (!string.IsNullOrWhiteSpace(tx.Reference))
        {
            // Flujo del WIDGET / Checkout Web embebido (sin payment_link_id, con reference).
            correlation = $"reference {tx.Reference}";
            appointmentId = await paymentRepository.ApplyTransactionUpdateByReferenceAsync(
                tx.Reference, status, tx.Id, paidAt, markAppointmentPaid: approved);
        }
        else
        {
            return true; // transacción sin enlace ni referencia (no es un pago de cita)
        }

        if (appointmentId is null)
            logger.LogWarning("Webhook Wompi: no se encontró el pago para {Correlation}", correlation);
        else
            logger.LogInformation("Webhook Wompi: cita {AppointmentId} -> pago {Status}", appointmentId, status);

        return true;
    }

    private async Task<bool> TrySendPaymentEmailAsync(AppointmentFlatDto appointment, string checkoutUrl, decimal amountUsd, decimal amountCop)
    {
        try
        {
            if (appointment.PatientId is null)
            {
                logger.LogWarning("No se envía email de pago: la cita {Id} no tiene paciente", appointment.Id);
                return false;
            }

            var patient = await patientService.GetByIdAsync(appointment.PatientId.Value);
            if (patient is null || string.IsNullOrWhiteSpace(patient.Email))
                return false;

            var user = await userService.GetByIdAsync(appointment.UserId);
            var office = user is null ? null : await officeService.GetByIdAsync(user.Office.Id);

            var dto = new AppointmentPaymentEmailDto
            {
                ToName = $"{patient.Name} {patient.Lastname}".Trim(),
                OfficeName = office?.Name ?? "ConfirmaMed",
                OfficeAddress = office?.Address ?? string.Empty,
                DoctorName = $"Dr. {appointment.DoctorName} {appointment.DoctorLastname}".Trim(),
                SpecialityName = appointment.SpecialityName,
                DateAppointment = appointment.DateAppointment,
                StartHour = appointment.StartHour,
                EndHour = appointment.EndHour,
                AmountUsd = amountUsd.ToString("N2", CultureInfo.InvariantCulture),
                AmountCop = amountCop.ToString("N0", CultureInfo.InvariantCulture),
                CheckoutUrl = checkoutUrl
            };

            var html = PaymentEmailTemplateHelper.BuildPaymentEmail(dto);

            return await birdEmailService.SendEmailAssignationAsync(
                patient.Email.Trim(), dto.ToName, "Pago de tu cita médica", html);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el email de pago de la cita {Id}", appointment.Id);
            return false;
        }
    }
}
