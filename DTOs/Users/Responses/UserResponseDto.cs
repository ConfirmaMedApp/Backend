using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.Offices.Responses;

namespace Backend.DTOs.Users.Responses;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public OfficeMinimalDto Office { get; set; } = new();
    public DoctorMinimalDto? Doctor { get; set; }
    public bool Status { get; set; }
    public string Role { get; set; } = string.Empty;
}
