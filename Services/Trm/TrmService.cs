using System.Globalization;
using System.Net.Http.Json;
using Backend.DTOs.Trm.Responses;
using Backend.Entities.Trm;
using Backend.Exceptions.BadRequest;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Backend.Services.Trm;

public class TrmService(HttpClient http, IMemoryCache cache, IOptions<TrmSettings> options, ILogger<TrmService> logger) : ITrmService
{
    private readonly TrmSettings _settings = options.Value;
    private const string CacheKey = "trm_usd_cop";

    public async Task<decimal> GetUsdToCopAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue<decimal>(CacheKey, out var cached) && cached > 0)
            return cached;

        try
        {
            // Último registro vigente del dataset oficial.
            var url = $"{_settings.BaseUrl}?$order=vigenciadesde%20DESC&$limit=1";
            var records = await http.GetFromJsonAsync<List<TrmRecordDto>>(url, ct);

            var raw = records?.FirstOrDefault()?.Valor;

            if (!string.IsNullOrWhiteSpace(raw) &&
                decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) &&
                rate > 0)
            {
                cache.Set(CacheKey, rate, TimeSpan.FromHours(Math.Max(1, _settings.CacheHours)));
                return rate;
            }

            logger.LogError("TRM: respuesta sin valor utilizable ({Raw})", raw);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TRM: no se pudo obtener la tasa oficial");
        }

        if (_settings.FallbackRate > 0)
        {
            logger.LogWarning("TRM: usando tasa de respaldo {Rate}", _settings.FallbackRate);
            return _settings.FallbackRate;
        }

        throw new BadRequestException("No se pudo obtener la TRM (tasa USD→COP) en este momento");
    }
}
