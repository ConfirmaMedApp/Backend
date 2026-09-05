using Backend.DTOs.DocumentTypes.Responses;
using Backend.DTOs.Genders.Responses;

namespace Backend.DTOs.Patients.Responses;

public class PatientResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Birthdate { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public DocumentTypeResponseDto DocumentType { get; set; } = new DocumentTypeResponseDto();
    public GenderResponseDto Gender { get; set; } = new GenderResponseDto();
}
