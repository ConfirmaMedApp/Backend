namespace Backend.DTOs.Users.Requests;

public class UserAvatarRequestDto
{
    public string? PresetKey { get; set; }
    public IFormFile? File { get; set; }
}
