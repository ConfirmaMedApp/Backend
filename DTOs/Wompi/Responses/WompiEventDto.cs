using System.Text.Json.Serialization;

namespace Backend.DTOs.Wompi.Responses;

public sealed class WompiEventDto
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public WompiEventData Data { get; set; } = new();

    [JsonPropertyName("signature")]
    public WompiEventSignature Signature { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("sent_at")]
    public string? SentAt { get; set; }
}

public sealed class WompiEventData
{
    [JsonPropertyName("transaction")]
    public WompiTransaction? Transaction { get; set; }
}

public sealed class WompiTransaction
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("amount_in_cents")]
    public long AmountInCents { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("customer_email")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("payment_link_id")]
    public string? PaymentLinkId { get; set; }
}

public sealed class WompiEventSignature
{
    [JsonPropertyName("checksum")]
    public string Checksum { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public List<string> Properties { get; set; } = new();
}
