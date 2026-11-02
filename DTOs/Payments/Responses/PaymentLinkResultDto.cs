namespace Backend.DTOs.Payments.Responses;

public sealed class PaymentLinkResultDto
{
    public int AppointmentId { get; set; }
    public string PaymentLinkId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;

    // Monto que se muestra en USD y su equivalente cobrado en COP.
    public decimal AmountUsd { get; set; }
    public decimal Trm { get; set; }
    public decimal AmountCop { get; set; }
    public long AmountInCents { get; set; }
    public string Currency { get; set; } = "COP";

    public DateTimeOffset? ExpiresAt { get; set; }
    public bool EmailSent { get; set; }
}
