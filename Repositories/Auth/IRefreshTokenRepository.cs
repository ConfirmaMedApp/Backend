using Backend.Entities.Auth;

namespace Backend.Repositories.Auth;

public interface IRefreshTokenRepository
{
    // Abre una nueva familia de sesión (login). Devuelve el id de la fila creada.
    Task<long> CreateAsync(
        int userId,
        string tokenHash,
        Guid sessionId,
        DateTime expiresAt,
        DateTime absoluteExpiresAt,
        string? ip,
        string? userAgent);

    Task<RefreshToken?> GetByHashAsync(string tokenHash);

    // Rotación atómica: revoca el viejo, lo encadena al nuevo y reutiliza la
    // familia y el tope absoluto. Devuelve el id del nuevo token, o null si el
    // viejo ya no existe. El nuevo absolute_expires_at lo fija la función en BD.
    Task<long?> RotateAsync(
        string oldHash,
        string newHash,
        DateTime newExpiresAt,
        string? ip,
        string? userAgent);

    // Revoca un único token (logout del dispositivo actual).
    Task<int> RevokeAsync(string tokenHash);

    // Revoca toda la familia de la sesión (detección de reúso / logout global).
    Task<int> RevokeFamilyAsync(Guid sessionId);
}
