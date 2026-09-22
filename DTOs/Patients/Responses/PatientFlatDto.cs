namespace Backend.DTOs.Patients.Responses;

public class PatientFlatDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Birthdate { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public int DocumentTypeId { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public int GenderId { get; set; }
    public string GenderName { get; set; } = string.Empty;
    public bool Status { get; set; }
}
