namespace Backend.Entities.Auth;

// Espejo de una fila de refresh_tokens, tal como la devuelve
// get_refresh_token_by_hash (columnas en minúscula -> Dapper mapea por nombre
// sin distinguir mayúsculas).
public class RefreshToken
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = null!;

    // Familia de la sesión: todos los tokens rotados comparten este id.
    public Guid SessionId { get; set; }

    // Expiración deslizante: se renueva en cada rotación.
    public DateTime ExpiresAt { get; set; }

    // Tope absoluto de la sesión: no se extiende al rotar.
    public DateTime AbsoluteExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByHash { get; set; }

    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsAbsolutelyExpired => DateTime.UtcNow >= AbsoluteExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired && !IsAbsolutelyExpired;
}
