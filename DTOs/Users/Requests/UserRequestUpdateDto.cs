namespace Backend.DTOs.Users.Requests;

public class UserRequestUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public bool Status { get; set; }
}
