using Backend.Entities.Auth;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.Auth;

public class RefreshTokenRepository(IDbConnectionFactory dbConnectionFactory)
    : RepositoryGuard, IRefreshTokenRepository
{
    public Task<long> CreateAsync(
        int userId,
        string tokenHash,
        Guid sessionId,
        DateTime expiresAt,
        DateTime absoluteExpiresAt,
        string? ip,
        string? userAgent)
    {
        const string command =
            "SELECT create_refresh_token(@UserId, @TokenHash, @SessionId, @ExpiresAt, @AbsoluteExpiresAt, @Ip, @UserAgent);";
        return ExecuteSafeAsync(async conn => await conn.ExecuteScalarAsync<long>(command, new
        {
            UserId = userId,
            TokenHash = tokenHash,
            SessionId = sessionId,
            ExpiresAt = expiresAt,
            AbsoluteExpiresAt = absoluteExpiresAt,
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

    public Task<long?> RotateAsync(
        string oldHash,
        string newHash,
        DateTime newExpiresAt,
        string? ip,
        string? userAgent)
    {
        const string command =
            "SELECT rotate_refresh_token(@OldHash, @NewHash, @NewExpiresAt, @Ip, @UserAgent);";
        return ExecuteSafeAsync(async conn => await conn.ExecuteScalarAsync<long?>(command, new
        {
            OldHash = oldHash,
            NewHash = newHash,
            NewExpiresAt = newExpiresAt,
            Ip = ip,
            UserAgent = userAgent
        }), dbConnectionFactory);
    }

    public Task<int> RevokeAsync(string tokenHash)
    {
        const string command = "SELECT revoke_refresh_token(@TokenHash);";
        return ExecuteSafeAsync(async conn =>
            await conn.ExecuteScalarAsync<int>(command, new { TokenHash = tokenHash }),
            dbConnectionFactory);
    }

    public Task<int> RevokeFamilyAsync(Guid sessionId)
    {
        const string command = "SELECT revoke_refresh_token_family(@SessionId);";
        return ExecuteSafeAsync(async conn =>
            await conn.ExecuteScalarAsync<int>(command, new { SessionId = sessionId }),
            dbConnectionFactory);
    }
}
