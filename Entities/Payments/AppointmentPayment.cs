namespace Backend.Entities.Payments;

public class AppointmentPayment
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public string? WompiPaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public decimal AmountUsd { get; set; }
    public decimal TrmUsed { get; set; }
    public long AmountInCents { get; set; }
    public string Currency { get; set; } = "COP";
    public string? WompiTransactionId { get; set; }
    public string? WompiReference { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime? ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
