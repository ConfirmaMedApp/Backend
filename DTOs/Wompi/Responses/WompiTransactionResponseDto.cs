using System.Text.Json.Serialization;

namespace Backend.DTOs.Wompi.Responses;

// Respuesta de GET {BaseUrl}transactions/{id} de Wompi: { "data": { ...transacción... } }.
// Reutiliza WompiTransaction (definido en WompiEventDto.cs) porque los campos
// (id, status, reference, payment_link_id, amount_in_cents) son los mismos.
public sealed class WompiTransactionResponseDto
{
    [JsonPropertyName("data")]
    public WompiTransaction? Data { get; set; }
}
