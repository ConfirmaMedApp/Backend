namespace Backend.DTOs.Patients.Responses;

public class PatientMinimalDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
}
