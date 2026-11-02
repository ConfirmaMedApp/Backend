namespace Backend.DTOs.Payments.Responses;

// Uso interno: precio vigente de la cita (con respaldo al precio del combo
// doctor+especialidad) y su estado de pago.
public sealed class AppointmentPaymentInfoDto
{
    public int AppointmentId { get; set; }
    public int DoctorId { get; set; }
    public int SpecialityId { get; set; }
    public decimal? PriceUsd { get; set; }
    public string PaymentStatus { get; set; } = "UNPAID";
}
