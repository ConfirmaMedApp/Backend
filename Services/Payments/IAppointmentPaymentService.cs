using Backend.DTOs.Payments.Responses;

namespace Backend.Services.Payments;

public interface IAppointmentPaymentService
{
    // Genera (o regenera) el enlace de pago Wompi de una cita y, opcionalmente, lo envía por email.
    Task<PaymentLinkResultDto> GeneratePaymentLinkAsync(int appointmentId, bool sendEmail, CancellationToken ct = default);

    // "Valores de la cita": precio en USD + conversión a COP con la TRM vigente.
    Task<AppointmentPricingResponseDto> GetPricingAsync(int appointmentId, CancellationToken ct = default);

    // Estado de pago de la cita + último enlace generado.
    Task<AppointmentPaymentStatusResponseDto> GetPaymentStatusAsync(int appointmentId);

    // Procesa un webhook de Wompi (verifica firma incluida). Devuelve true si se recibió y procesó.
    Task<bool> HandleWebhookAsync(string rawBody, string? checksumHeader);
}
