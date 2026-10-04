using Backend.DTOs.Auth.Requests;
using Backend.DTOs.Auth.Responses;

namespace Backend.Services.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(AuthRequestLoginDto dto, HttpRequest request, HttpResponse response);
    Task<AuthResponseDto> RefreshAsync(HttpRequest request, HttpResponse response);
    Task LogoutAsync(HttpRequest request, HttpResponse response);
    AuthResponseDto VerifyToken(string? token);
}
