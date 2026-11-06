namespace Backend.Entities.Trm;

public class TrmSettings
{
    // Dataset oficial de la TRM (Superfinanciera) en datos.gov.co.
    public string BaseUrl { get; set; } = "https://www.datos.gov.co/resource/32sa-8pi3.json";

    // Cuántas horas cachear la TRM antes de volver a consultarla.
    public int CacheHours { get; set; } = 12;

    // Tasa de respaldo (COP por USD) si la API de la TRM no responde o no trae
    // un valor usable. 0 = sin respaldo => error. Por defecto 4000 COP/USD.
    public decimal FallbackRate { get; set; } = 4000m;
}
