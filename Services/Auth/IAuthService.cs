using Backend.DTOs.Auth.Requests;
using Backend.DTOs.Auth.Responses;

namespace Backend.Services.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(AuthRequestLoginDto dto, HttpResponse response);
    AuthResponseDto VerifyToken(string? token);
    Task Logout(HttpResponse response);
}
