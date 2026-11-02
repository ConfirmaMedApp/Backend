using System.Text.Json.Serialization;

namespace Backend.DTOs.Wompi.Requests;

public sealed class CreatePaymentLinkRequestDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    // true = acepta una sola transacción APROBADA (ideal para una cita).
    [JsonPropertyName("single_use")]
    public bool SingleUse { get; set; } = true;

    [JsonPropertyName("collect_shipping")]
    public bool CollectShipping { get; set; } = false;

    // Por ahora Wompi Colombia solo acepta "COP".
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "COP";

    // Monto fijo en centavos de COP.
    [JsonPropertyName("amount_in_cents")]
    public long AmountInCents { get; set; }

    // ISO 8601 en UTC.
    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }

    [JsonPropertyName("redirect_url")]
    public string? RedirectUrl { get; set; }
}
