namespace Backend.DTOs.Patients.Requests;

public class PatientRequestCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Birthdate { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public int DocumentTypeId { get; set; }
    public int GenderId { get; set; }
    public bool Status { get; set; } = true;
}