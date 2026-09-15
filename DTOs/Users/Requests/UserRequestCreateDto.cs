namespace Backend.DTOs.Users.Requests;

public class UserRequestCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public bool Status { get; set; }
    public string Role { get; set; } = string.Empty;
}
