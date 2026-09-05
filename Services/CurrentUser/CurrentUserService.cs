using System.Security.Claims;
using Backend.Exceptions.Unauthorized;

namespace Backend.Services.CurrentUser;

public class CurrentUserService : ICurrentUserService
{
    public int? UserId { get; }

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var userIdClaim = httpContextAccessor.HttpContext?
            .User?
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            UserId = userId;
        }
        else
        {
            UserId = null;
        }
    }
}