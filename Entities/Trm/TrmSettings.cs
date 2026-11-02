namespace Backend.Entities.Trm;

public class TrmSettings
{
    // Dataset oficial de la TRM (Superfinanciera) en datos.gov.co.
    public string BaseUrl { get; set; } = "https://www.datos.gov.co/resource/32sa-8pi3.json";

    // Cuántas horas cachear la TRM antes de volver a consultarla.
    public int CacheHours { get; set; } = 12;

    // Tasa de respaldo si la API no responde (0 = sin respaldo => error).
    public decimal FallbackRate { get; set; } = 0m;
}
