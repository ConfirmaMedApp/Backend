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

        await IssueRefreshTokenAsync(user.Id, request, response);

        return authResponse;
    }

    public async Task<AuthResponseDto> RefreshAsync(HttpRequest request, HttpResponse response)
    {
        var rawToken = request.Cookies[GetRefreshCookieName()];
        if (string.IsNullOrEmpty(rawToken))
            throw new UnauthorizedException("Sesión no válida");

        var tokenHash = RefreshTokenHelper.Hash(rawToken);
        var stored = await refreshTokenRepository.GetByHashAsync(tokenHash)
                     ?? throw new UnauthorizedException("Sesión no válida");

        // Detección de reúso: un refresh ya revocado que vuelve a usarse es
        // señal de robo -> se revocan todos los tokens activos del usuario.
        if (stored.IsRevoked)
        {
            await refreshTokenRepository.RevokeAllForUserAsync(stored.UserId);
            DeleteRefreshCookie(response);
            throw new UnauthorizedException("Sesión no válida");
        }

        if (stored.IsExpired)
        {
            DeleteRefreshCookie(response);
            throw new UnauthorizedException("Sesión expirada");
        }

        var user = await userRepository.GetByIdAsync(stored.UserId)
                   ?? throw new UnauthorizedException("Sesión no válida");

        // Rotación: revocar el token actual y emitir uno nuevo.
        var newRawToken = await IssueRefreshTokenAsync(stored.UserId, request, response);
        await refreshTokenRepository.RevokeAsync(tokenHash, RefreshTokenHelper.Hash(newRawToken));

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
            await refreshTokenRepository.RevokeAsync(RefreshTokenHelper.Hash(rawToken), null);
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

    // Crea el refresh token, lo persiste hasheado y setea la cookie httpOnly.
    // Devuelve el token en claro (necesario para calcular el hash de linaje al rotar).
    private async Task<string> IssueRefreshTokenAsync(int userId, HttpRequest request, HttpResponse response)
    {
        var rawToken = RefreshTokenHelper.Generate();
        var expiresInDays = GetRefreshTokenExpireDays();

        var ip = request.HttpContext.Connection.RemoteIpAddress?.ToString();
        if (ip is { Length: > 64 }) ip = ip[..64];

        var userAgent = request.Headers.UserAgent.ToString();

        await refreshTokenRepository.CreateAsync(
            userId,
            RefreshTokenHelper.Hash(rawToken),
            expiresInDays,
            ip,
            string.IsNullOrEmpty(userAgent) ? null : userAgent);

        SetRefreshCookie(response, rawToken, expiresInDays);

        return rawToken;
    }

    private void SetRefreshCookie(HttpResponse response, string rawToken, int expiresInDays)
    {
        response.Cookies.Append(GetRefreshCookieName(), rawToken, BuildCookieOptions(expiresInDays));
    }

    private void DeleteRefreshCookie(HttpResponse response)
    {
        // Debe compartir Path/SameSite/Secure con la cookie original para que el navegador la borre.
        var options = BuildCookieOptions(0);
        options.Expires = DateTimeOffset.UnixEpoch;
        response.Cookies.Delete(GetRefreshCookieName(), options);
    }

    private CookieOptions BuildCookieOptions(int expiresInDays)
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
            Expires = DateTimeOffset.UtcNow.AddDays(expiresInDays)
        };
    }

    private string GetRefreshCookieName()
    {
        return configuration["Jwt:RefreshCookieName"]
               ?? Environment.GetEnvironmentVariable("Jwt__RefreshCookieName")
               ?? "cm_rt";
    }

    private int GetRefreshTokenExpireDays()
    {
        return configuration.GetValue<int?>("Jwt:RefreshTokenExpireDays")
               ?? (int.TryParse(
                       Environment.GetEnvironmentVariable("Jwt__RefreshTokenExpireDays"),
                       out var envDays)
                   ? envDays
                   : 7);
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
