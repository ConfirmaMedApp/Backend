namespace Backend.Entities.Wompi;

public class WompiSettings
{
    // Sandbox: https://sandbox.wompi.co/v1/  |  Producción: https://production.wompi.co/v1/
    public string BaseUrl { get; set; } = "https://sandbox.wompi.co/v1/";

    public string PublicKey { get; set; } = string.Empty;

    // Llave privada: requerida para crear enlaces de pago.
    public string PrivateKey { get; set; } = string.Empty;

    // Secreto de eventos: para verificar la firma de los webhooks.
    public string EventsSecret { get; set; } = string.Empty;

    // A dónde vuelve el paciente tras pagar (página del frontend).
    public string RedirectUrl { get; set; } = string.Empty;

    // Vigencia del enlace de pago en horas.
    public int ExpirationHours { get; set; } = 72;

    // Base del enlace compartible que ve el paciente.
    public string CheckoutBaseUrl { get; set; } = "https://checkout.wompi.co/l/";
}
