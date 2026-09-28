using Backend.Entities.Auth;

namespace Backend.Repositories.Auth;

public interface IRefreshTokenRepository
{
    Task<int> CreateAsync(int userId, string tokenHash, int expiresInDays, string? ip, string? userAgent);
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task RevokeAsync(string tokenHash, string? replacedByHash);
    Task RevokeAllForUserAsync(int userId);
}
