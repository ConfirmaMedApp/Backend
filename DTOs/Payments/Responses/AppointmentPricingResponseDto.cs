namespace Backend.DTOs.Payments.Responses;

// "Valores de la cita": precio en USD y su conversión a COP con la TRM vigente.
public sealed class AppointmentPricingResponseDto
{
    public int AppointmentId { get; set; }
    public decimal? PriceUsd { get; set; }
    public decimal Trm { get; set; }
    public decimal PriceCop { get; set; }
    public string Currency { get; set; } = "COP";
    public string PaymentStatus { get; set; } = "UNPAID";
}
