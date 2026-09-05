namespace Backend.DTOs.Auth.Requests;

public class AuthRequestLoginDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
