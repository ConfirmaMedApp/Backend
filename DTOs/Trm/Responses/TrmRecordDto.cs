using System.Text.Json.Serialization;

namespace Backend.DTOs.Trm.Responses;

// Registro del dataset de TRM de datos.gov.co (Superfinanciera).
public sealed class TrmRecordDto
{
    [JsonPropertyName("valor")]
    public string Valor { get; set; } = string.Empty;

    [JsonPropertyName("unidad")]
    public string? Unidad { get; set; }

    [JsonPropertyName("vigenciadesde")]
    public string? VigenciaDesde { get; set; }

    [JsonPropertyName("vigenciahasta")]
    public string? VigenciaHasta { get; set; }
}
