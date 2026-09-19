namespace Backend.DTOs.Users.Responses;

public class UserFlatDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string OfficeNit { get; set; } = string.Empty;
    public int? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorLastName { get; set; }
    public string? DoctorDocument { get; set; }
    public bool Status { get; set; }
    public string Role { get; set; } = string.Empty;
}
