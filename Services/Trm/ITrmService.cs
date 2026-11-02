namespace Backend.Services.Trm;

public interface ITrmService
{
    // Devuelve la TRM vigente (COP por 1 USD), cacheada a diario.
    Task<decimal> GetUsdToCopAsync(CancellationToken ct = default);
}
