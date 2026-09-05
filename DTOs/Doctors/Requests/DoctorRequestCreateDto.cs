namespace Backend.DTOs.Doctors.Requests;

public class DoctorRequestCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public int DocumentTypeId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Status { get; set; }
}