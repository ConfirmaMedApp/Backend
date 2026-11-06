using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Backend.DTOs.Wompi.Requests;
using Backend.DTOs.Wompi.Responses;
using Backend.Entities.Wompi;
using Backend.Exceptions.BadRequest;
using Microsoft.Extensions.Options;

namespace Backend.Services.Wompi;

public class WompiService(HttpClient http, IOptions<WompiSettings> options, ILogger<WompiService> logger) : IWompiService
{
    private readonly WompiSettings _settings = options.Value;

    public async Task<PaymentLinkData> CreatePaymentLinkAsync(
        string name,
        string description,
        long amountInCents,
        DateTimeOffset? expiresAt,
        string? redirectUrl,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.PrivateKey))
            throw new BadRequestException("Wompi no está configurado (falta la llave privada)");

        var payload = new CreatePaymentLinkRequestDto
        {
            Name = Truncate(name, 64),
            Description = Truncate(description, 255),
            SingleUse = true,
            CollectShipping = false,
            Currency = "COP",
            AmountInCents = amountInCents,
            ExpiresAt = expiresAt?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            RedirectUrl = string.IsNullOrWhiteSpace(redirectUrl) ? null : redirectUrl
        };

        var response = await http.PostAsJsonAsync("payment_links", payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Wompi CreatePaymentLink falló ({Status}): {Body}", response.StatusCode, body);
            throw new BadRequestException($"No se pudo crear el enlace de pago ({(int)response.StatusCode})");
        }

        var result = await response.Content.ReadFromJsonAsync<PaymentLinkResponseDto>(cancellationToken: ct);

        if (result?.Data is null || string.IsNullOrWhiteSpace(result.Data.Id))
            throw new BadRequestException("Respuesta vacía de Wompi al crear el enlace de pago");

        return result.Data;
    }

    public async Task<WompiTransaction?> GetTransactionAsync(string transactionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return null;

        if (string.IsNullOrWhiteSpace(_settings.PrivateKey))
            throw new BadRequestException("Wompi no está configurado (falta la llave privada)");

        var response = await http.GetAsync($"transactions/{Uri.EscapeDataString(transactionId)}", ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Wompi GetTransaction {Id} falló ({Status}): {Body}", transactionId, response.StatusCode, body);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<WompiTransactionResponseDto>(cancellationToken: ct);
        return result?.Data;
    }

    public string BuildCheckoutUrl(string paymentLinkId)
    {
        var baseUrl = _settings.CheckoutBaseUrl.EndsWith('/')
            ? _settings.CheckoutBaseUrl
            : _settings.CheckoutBaseUrl + "/";

        return $"{baseUrl}{paymentLinkId}";
    }

    public string BuildIntegritySignature(string reference, long amountInCents, string currency)
    {
        if (string.IsNullOrWhiteSpace(_settings.IntegritySecret))
            throw new BadRequestException("Wompi no está configurado para el widget (falta el secreto de integridad)");

        // OJO: Wompi exige la firma de integridad en hex MINÚSCULA
        // (distinto del checksum del webhook, que va en mayúscula).
        var toSign = $"{reference}{amountInCents}{currency}{_settings.IntegritySecret}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(toSign));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool VerifyEventSignature(string rawBody, string? checksumHeader)
    {
        if (string.IsNullOrWhiteSpace(_settings.EventsSecret))
        {
            logger.LogWarning("Wompi EventsSecret no configurado; no se puede verificar la firma del webhook");
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("signature", out var signature) ||
                !signature.TryGetProperty("properties", out var properties) ||
                properties.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("data", out var data))
            {
                return false;
            }

            var builder = new StringBuilder();

            // 1) valores de las propiedades indicadas, en orden
            foreach (var property in properties.EnumerateArray())
            {
                var path = property.GetString();
                if (!string.IsNullOrEmpty(path))
                    builder.Append(ResolvePath(data, path));
            }

            // 2) timestamp del evento
            if (root.TryGetProperty("timestamp", out var timestamp))
            {
                builder.Append(timestamp.ValueKind == JsonValueKind.Number
                    ? timestamp.GetRawText()
                    : timestamp.GetString());
            }

            // 3) secreto de eventos
            builder.Append(_settings.EventsSecret);

            var computed = Sha256Hex(builder.ToString());

            var expected = checksumHeader;
            if (string.IsNullOrWhiteSpace(expected) &&
                signature.TryGetProperty("checksum", out var checksum))
            {
                expected = checksum.GetString();
            }

            if (string.IsNullOrWhiteSpace(expected))
                return false;

            return FixedTimeEquals(computed, expected);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando la firma del webhook de Wompi");
            return false;
        }
    }

    // Navega `data` siguiendo una ruta tipo "transaction.status" y devuelve el valor escalar.
    private static string ResolvePath(JsonElement data, string path)
    {
        var current = data;
        foreach (var segment in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next))
                return string.Empty;
            current = next;
        }

        return current.ValueKind == JsonValueKind.String
            ? current.GetString() ?? string.Empty
            : current.GetRawText();
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes); // mayúsculas
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a.ToUpperInvariant());
        var bb = Encoding.UTF8.GetBytes(b.ToUpperInvariant());
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    private static string Truncate(string value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
