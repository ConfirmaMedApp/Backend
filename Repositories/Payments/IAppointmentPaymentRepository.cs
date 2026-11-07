using Backend.DTOs.Payments.Responses;
using Backend.Entities.Payments;

namespace Backend.Repositories.Payments;

public interface IAppointmentPaymentRepository
{
    Task<int> CreateAsync(AppointmentPayment payment);

    Task<AppointmentPayment?> GetByPaymentLinkIdAsync(string paymentLinkId);

    Task<AppointmentPayment?> GetLatestByAppointmentIdAsync(int appointmentId);

    // Precio vigente de la cita (price_usd de la cita, o del combo doctor+especialidad) + estado.
    Task<AppointmentPaymentInfoDto?> GetAppointmentPaymentInfoAsync(int appointmentId);

    // Aplica el resultado de una transacción (correlacionada por payment_link_id) al pago
    // y, si fue aprobada, marca la cita como pagada. Flujo de ENLACES de pago.
    // Devuelve el appointment_id afectado (null si no existe el pago) y si la cita
    // ACABA de pasar a pagada en esta llamada (NewlyPaid; false en reintentos del webhook).
    Task<(int? AppointmentId, bool NewlyPaid)> ApplyTransactionUpdateByLinkIdAsync(
        string paymentLinkId,
        string status,
        string? transactionId,
        string? reference,
        DateTime? paidAt,
        bool markAppointmentPaid);

    // Igual, pero correlacionando por la referencia del pago. Flujo del WIDGET / Checkout Web
    // embebido (la transacción no trae payment_link_id, solo la reference que generó el backend).
    Task<(int? AppointmentId, bool NewlyPaid)> ApplyTransactionUpdateByReferenceAsync(
        string reference,
        string status,
        string? transactionId,
        DateTime? paidAt,
        bool markAppointmentPaid);
}
