using Backend.DTOs.Auth.Requests;
using Backend.DTOs.Auth.Responses;
using Backend.Exceptions.NotFound;
using Backend.Exceptions.Unauthorized;
using Backend.Repositories.Users;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Services.Auth;

public class AuthService(IUserRepository userRepository, IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponseDto> LoginAsync(AuthRequestLoginDto dto, HttpResponse response)
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

        return authResponse;
    }

    public Task Logout(HttpResponse response)
    {
        return Task.CompletedTask;
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
