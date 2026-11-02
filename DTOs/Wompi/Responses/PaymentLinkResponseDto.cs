using System.Text.Json.Serialization;

namespace Backend.DTOs.Wompi.Responses;

public sealed class PaymentLinkResponseDto
{
    [JsonPropertyName("data")]
    public PaymentLinkData Data { get; set; } = new();
}

public sealed class PaymentLinkData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("single_use")]
    public bool SingleUse { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("amount_in_cents")]
    public long? AmountInCents { get; set; }

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }

    [JsonPropertyName("merchant_public_key")]
    public string MerchantPublicKey { get; set; } = string.Empty;
}
