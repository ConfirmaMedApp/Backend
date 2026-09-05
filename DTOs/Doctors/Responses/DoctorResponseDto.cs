using Backend.DTOs.DocumentTypes.Responses;

namespace Backend.DTOs.Doctors.Responses;

public class DoctorResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public DocumentTypeResponseDto DocumentType { get; set; } = new();
    public string Email { get; set; } = string.Empty;
    public bool Status { get; set; }
}