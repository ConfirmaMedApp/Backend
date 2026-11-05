namespace Backend.DTOs.Payments.Responses;

// Datos que necesita el frontend para renderizar el Widget / Checkout Web de Wompi.
// La firma (SignatureIntegrity) la calcula el backend; el frontend solo la reenvía.
public sealed class CheckoutConfigResultDto
{
    public int AppointmentId { get; set; }

    // Llave pública: es segura de exponer en el cliente.
    public string PublicKey { get; set; } = string.Empty;

    public string Currency { get; set; } = "COP";
    public long AmountInCents { get; set; }

    // Referencia única del pago (correlaciona el webhook con la cita).
    public string Reference { get; set; } = string.Empty;

    // signature:integrity que exige el Widget / Checkout Web (hex minúscula).
    public string SignatureIntegrity { get; set; } = string.Empty;

    // A dónde vuelve el paciente tras pagar (página del frontend).
    public string? RedirectUrl { get; set; }

    // Solo para mostrar en la UI (el cobro real va en AmountInCents/COP).
    public decimal AmountUsd { get; set; }
    public decimal Trm { get; set; }
    public decimal AmountCop { get; set; }
}
