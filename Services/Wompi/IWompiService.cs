using Backend.DTOs.Wompi.Responses;

namespace Backend.Services.Wompi;

public interface IWompiService
{
    Task<PaymentLinkData> CreatePaymentLinkAsync(
        string name,
        string description,
        long amountInCents,
        DateTimeOffset? expiresAt,
        string? redirectUrl,
        CancellationToken ct = default);

    // Enlace compartible que ve el paciente: https://checkout.wompi.co/l/{id}
    string BuildCheckoutUrl(string paymentLinkId);

    // Firma de integridad para el Widget / Checkout Web embebido:
    // SHA256(reference + amountInCents + currency + integritySecret) en hex minúscula.
    string BuildIntegritySignature(string reference, long amountInCents, string currency);

    // Verifica la firma de un webhook de Wompi (header X-Event-Checksum / signature.checksum).
    bool VerifyEventSignature(string rawBody, string? checksumHeader);

    // Consulta una transacción por su id en Wompi (GET transactions/{id}). Se usa para
    // reconciliar el estado del pago cuando el webhook no llega (polling). Devuelve null
    // si no existe o si Wompi responde con error.
    Task<WompiTransaction?> GetTransactionAsync(string transactionId, CancellationToken ct = default);
}
