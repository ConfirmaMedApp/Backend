using Backend.Entities.Auth;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.Auth;

public class RefreshTokenRepository(IDbConnectionFactory dbConnectionFactory)
    : RepositoryGuard, IRefreshTokenRepository
{
    public Task<int> CreateAsync(int userId, string tokenHash, int expiresInDays, string? ip, string? userAgent)
    {
        const string command =
            "SELECT create_refresh_token(@UserId, @TokenHash, @ExpiresInDays, @Ip, @UserAgent);";
        return ExecuteSafeAsync(async conn => await conn.ExecuteScalarAsync<int>(command, new
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresInDays = expiresInDays,
            Ip = ip,
            UserAgent = userAgent
        }), dbConnectionFactory);
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash)
    {
        const string query = "SELECT * FROM get_refresh_token_by_hash(@TokenHash);";
        return ExecuteSafeAsync(async conn =>
            await conn.QueryFirstOrDefaultAsync<RefreshToken>(query, new { TokenHash = tokenHash }),
            dbConnectionFactory);
    }

    public Task RevokeAsync(string tokenHash, string? replacedByHash)
    {
        const string command = "SELECT revoke_refresh_token(@TokenHash, @ReplacedBy);";
        return ExecuteSafeAsync(async conn =>
            await conn.ExecuteAsync(command, new { TokenHash = tokenHash, ReplacedBy = replacedByHash }),
            dbConnectionFactory);
    }

    public Task RevokeAllForUserAsync(int userId)
    {
        const string command = "SELECT revoke_all_refresh_tokens_for_user(@UserId);";
        return ExecuteSafeAsync(async conn =>
            await conn.ExecuteAsync(command, new { UserId = userId }),
            dbConnectionFactory);
    }
}
