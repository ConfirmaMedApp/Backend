namespace Backend.Entities.Patients;

public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime Birthdate { get; set; }
    public string Document { get; set; } = string.Empty;
    public int DocumentTypeId { get; set; }
    public int GenderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
