namespace Backend.DTOs.Payments.Responses;

public sealed class AppointmentPaymentStatusResponseDto
{
    public int AppointmentId { get; set; }
    public string PaymentStatus { get; set; } = "UNPAID";

    // Último enlace/transacción generada para la cita (si existe).
    public string? PaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? TransactionStatus { get; set; }
    public decimal? AmountUsd { get; set; }
    public decimal? AmountCop { get; set; }
    public string Currency { get; set; } = "COP";
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}
