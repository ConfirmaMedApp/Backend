using Backend.DTOs.Auth.Requests;
using Backend.DTOs.Auth.Responses;
using Backend.Exceptions.NotFound;
using Backend.Exceptions.Unauthorized;
using Backend.Helpers;
using Backend.Repositories.Auth;
using Backend.Repositories.Users;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Services.Auth;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IConfiguration configuration,
    IWebHostEnvironment environment) : IAuthService
{
    public async Task<AuthResponseDto> LoginAsync(AuthRequestLoginDto dto, HttpRequest request, HttpResponse response)
    {
        var user = await userRepository.GetByUsernameAsync(dto.UserName)
                   ?? throw new UnauthorizedException("Credenciales invalidas");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            throw new UnauthorizedException("Credenciales invalidas");

        var authResponse = new AuthResponseDto
        {
            Id = user.Id,
            FullName = $"{user.Name} {user.Lastname}",
            UserName = dto.UserName,
            Role = user.Role
        };

        authResponse.Token = GenerateJwtToken(authResponse);

        // Abre una nueva familia de sesión y entrega la cookie del refresh token.
        await IssueNewSessionAsync(user.Id, request, response);

        return authResponse;
    }

    public async Task<AuthResponseDto> RefreshAsync(HttpRequest request, HttpResponse response)
    {
        var rawToken = request.Cookies[GetRefreshCookieName()];
        if (string.IsNullOrEmpty(rawToken))
            throw new UnauthorizedException("Sesión no válida");

        var tokenHash = RefreshTokenHelper.Hash(rawToken);
        var stored = await refreshTokenRepository.GetByHashAsync(tokenHash);
        if (stored is null)
        {
            DeleteRefreshCookie(response);
            throw new UnauthorizedException("Sesión no válida");
        }

        // Detección de reúso: un refresh ya revocado que vuelve a usarse es
        // señal de robo -> se revoca toda la familia de la sesión.
        if (stored.IsRevoked)
        {
            await refreshTokenRepository.RevokeFamilyAsync(stored.SessionId);
            DeleteRefreshCookie(response);
            throw new UnauthorizedException("Sesión no válida");
        }

        // Expiración deslizante (por token) o tope absoluto (de la sesión).
        if (stored.IsExpired || stored.IsAbsolutelyExpired)
        {
            await refreshTokenRepository.RevokeAsync(tokenHash);
            DeleteRefreshCookie(response);
            throw new UnauthorizedException("Sesión expirada");
        }

        var user = await userRepository.GetByIdAsync(stored.UserId);
        if (user is null)
        {
            await refreshTokenRepository.RevokeFamilyAsync(stored.SessionId);
            DeleteRefreshCookie(response);
            throw new UnauthorizedException("Sesión no válida");
        }

        // Rotación atómica en BD: revoca el viejo, lo encadena y emite uno nuevo
        // en la misma familia. La nueva expiración deslizante se recorta al tope
        // absoluto de la sesión para no superarlo.
        var newRawToken = RefreshTokenHelper.Generate();
        var newExpiresAt = ClampToAbsolute(DateTime.UtcNow.AddDays(GetRefreshTokenExpireDays()), stored.AbsoluteExpiresAt);
        var (ip, userAgent) = GetClientContext(request);

        var newId = await refreshTokenRepository.RotateAsync(
            tokenHash,
            RefreshTokenHelper.Hash(newRawToken),
            newExpiresAt,
            ip,
            userAgent);

        // Null => la fila desapareció entre la lectura y la rotación (carrera).
        if (newId is null)
        {
            DeleteRefreshCookie(response);
            throw new UnauthorizedException("Sesión no válida");
        }

        SetRefreshCookie(response, newRawToken, newExpiresAt);

        var authResponse = new AuthResponseDto
        {
            Id = user.Id,
            FullName = $"{user.Name} {user.Lastname}",
            UserName = user.Username,
            Role = user.Role
        };

        authResponse.Token = GenerateJwtToken(authResponse);

        return authResponse;
    }

    public async Task LogoutAsync(HttpRequest request, HttpResponse response)
    {
        var rawToken = request.Cookies[GetRefreshCookieName()];
        if (!string.IsNullOrEmpty(rawToken))
        {
            // Cierra la familia completa para invalidar cualquier token rotado vivo.
            var stored = await refreshTokenRepository.GetByHashAsync(RefreshTokenHelper.Hash(rawToken));
            if (stored is not null)
                await refreshTokenRepository.RevokeFamilyAsync(stored.SessionId);
        }

        DeleteRefreshCookie(response);
    }

    public AuthResponseDto VerifyToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedException("Token no encontrado");
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"]
                    ?? Environment.GetEnvironmentVariable("Jwt__Issuer")
                    ?? throw new NotFoundException("Issuer not found"),
                ValidAudience = configuration["Jwt:Audience"]
                    ?? Environment.GetEnvironmentVariable("Jwt__Audience")
                    ?? throw new NotFoundException("Audience not found"),
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            tokenHandler.ValidateToken(token, parameters, out SecurityToken validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            var userId = jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
            var name = jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value;
            var role = jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value;

            if (!int.TryParse(userId, out var userIdParsed))
            {
                throw new UnauthorizedException("ID de usuario inválido en el token");
            }

            var response = new AuthResponseDto
            {
                Id = userIdParsed,
                FullName = name,
                Role = role,
            };

            return response;
        }
        catch
        {
            throw new UnauthorizedException("Token inválido o expirado");
        }
    }

    // -------------------------------------------------------------------------
    // Helpers privados
    // -------------------------------------------------------------------------

    // Crea una nueva familia de sesión: genera el token, lo persiste hasheado con
    // sus expiraciones (deslizante + absoluta) y setea la cookie httpOnly.
    private async Task<string> IssueNewSessionAsync(int userId, HttpRequest request, HttpResponse response)
    {
        var rawToken = RefreshTokenHelper.Generate();
        var sessionId = Guid.NewGuid();

        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(GetRefreshTokenExpireDays());
        var absoluteExpiresAt = now.AddDays(GetRefreshTokenAbsoluteExpireDays());

        var (ip, userAgent) = GetClientContext(request);

        await refreshTokenRepository.CreateAsync(
            userId,
            RefreshTokenHelper.Hash(rawToken),
            sessionId,
            expiresAt,
            absoluteExpiresAt,
            ip,
            userAgent);

        SetRefreshCookie(response, rawToken, expiresAt);

        return rawToken;
    }

    private static (string? Ip, string? UserAgent) GetClientContext(HttpRequest request)
    {
        var ip = request.HttpContext.Connection.RemoteIpAddress?.ToString();
        if (ip is { Length: > 64 }) ip = ip[..64];

        var userAgent = request.Headers.UserAgent.ToString();

        return (ip, string.IsNullOrEmpty(userAgent) ? null : userAgent);
    }

    // La expiración deslizante nunca puede superar el tope absoluto de la sesión.
    private static DateTime ClampToAbsolute(DateTime sliding, DateTime absolute)
    {
        return sliding < absolute ? sliding : absolute;
    }

    private void SetRefreshCookie(HttpResponse response, string rawToken, DateTime expiresAt)
    {
        response.Cookies.Append(GetRefreshCookieName(), rawToken, BuildCookieOptions(expiresAt));
    }

    private void DeleteRefreshCookie(HttpResponse response)
    {
        // Debe compartir Path/SameSite/Secure con la cookie original para que el navegador la borre.
        var options = BuildCookieOptions(DateTimeOffset.UnixEpoch.UtcDateTime);
        options.Expires = DateTimeOffset.UnixEpoch;
        response.Cookies.Delete(GetRefreshCookieName(), options);
    }

    private CookieOptions BuildCookieOptions(DateTime expiresAt)
    {
        var isDev = environment.IsDevelopment();

        return new CookieOptions
        {
            HttpOnly = true,
            // En prod front y back están en dominios distintos (cross-site) -> None + Secure.
            // En dev (localhost sobre http) Lax + no-Secure permite el flujo sin HTTPS.
            Secure = !isDev,
            SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
            Path = "/",
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc))
        };
    }

    private string GetRefreshCookieName()
    {
        return configuration["Jwt:RefreshCookieName"]
               ?? Environment.GetEnvironmentVariable("Jwt__RefreshCookieName")
               ?? "cm_rt";
    }

    // Ventana deslizante del refresh token (dias). Default 7.
    private int GetRefreshTokenExpireDays()
    {
        return configuration.GetValue<int?>("Jwt:RefreshTokenExpireDays")
               ?? (int.TryParse(
                       Environment.GetEnvironmentVariable("Jwt__RefreshTokenExpireDays"),
                       out var envDays)
                   ? envDays
                   : 7);
    }

    // Tope absoluto de la sesión (dias). Default 30.
    private int GetRefreshTokenAbsoluteExpireDays()
    {
        return configuration.GetValue<int?>("Jwt:RefreshTokenAbsoluteExpireDays")
               ?? (int.TryParse(
                       Environment.GetEnvironmentVariable("Jwt__RefreshTokenAbsoluteExpireDays"),
                       out var envDays)
                   ? envDays
                   : 30);
    }

    private string GenerateJwtToken(AuthResponseDto dto)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]
                ?? Environment.GetEnvironmentVariable("Jwt__Key")
                ?? throw new NotFoundException("Key not found"))
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, dto.Id.ToString()),
            new Claim(ClaimTypes.Name, dto.FullName),
            new Claim(ClaimTypes.Role, dto.Role)
        };

        var expireMinutes = configuration.GetValue<int?>("Jwt:ExpireMinutes")
            ?? (int.TryParse(
                    Environment.GetEnvironmentVariable("Jwt__Expire__Minutes"),
                    out var envMinutes)
                ? envMinutes
                : throw new KeyNotFoundException("Jwt:ExpireMinutes no está configurado"));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"]
                ?? Environment.GetEnvironmentVariable("Jwt__Issuer")
                ?? throw new NotFoundException("Issuer not found"),
            audience: configuration["Jwt:Audience"]
                ?? Environment.GetEnvironmentVariable("Jwt__Audience")
                ?? throw new NotFoundException("Audience not found"),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
