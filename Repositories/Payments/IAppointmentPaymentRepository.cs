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

    // Aplica el resultado de una transacción al pago y, si fue aprobada, marca la cita como pagada.
    // Devuelve el appointment_id afectado, o null si no existe el pago.
    Task<int?> ApplyTransactionUpdateAsync(
        string paymentLinkId,
        string status,
        string? transactionId,
        string? reference,
        DateTime? paidAt,
        bool markAppointmentPaid);
}
