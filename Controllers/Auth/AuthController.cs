using System.Security.Claims;
using Backend.DTOs.Auth.Requests;
using Backend.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers.Auth;

[Authorize]
public class AuthController(IAuthService authService) : BaseControllerCustom
{
    [AllowAnonymous]
    [HttpPost("login", Name = "LoginUser")]
    public async Task<IActionResult> Login([FromBody] AuthRequestLoginDto dto)
    {
        var result = await authService.LoginAsync(dto, Request, Response);
        return OkResponse(result, "Sesión iniciada");
    }

    [AllowAnonymous]
    [HttpPost("refresh", Name = "RefreshToken")]
    public async Task<IActionResult> Refresh()
    {
        var result = await authService.RefreshAsync(Request, Response);
        return OkResponse(result, "Token renovado");
    }

    [AllowAnonymous]
    [HttpPost("logout", Name = "LogoutUser")]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync(Request, Response);
        return NoContentResponse();
    }

    [AllowAnonymous]
    [HttpGet("verify")]
    public IActionResult Verify()
    {
        string? authHeader = Request.Headers.Authorization;
        var token = authHeader?.Replace("Bearer ", "");
        
        var response = authService.VerifyToken(token);
        return OkResponse(response, "Token válido");
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        return OkResponse(new
        {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Name = User.Identity?.Name
        });
    }
}
