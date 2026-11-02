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

    // Verifica la firma de un webhook de Wompi (header X-Event-Checksum / signature.checksum).
    bool VerifyEventSignature(string rawBody, string? checksumHeader);
}
