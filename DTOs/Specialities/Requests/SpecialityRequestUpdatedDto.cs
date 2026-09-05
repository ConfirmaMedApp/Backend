namespace Backend.DTOs.Specialities.Requests;

public class SpecialityRequestUpdatedDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool Status { get; set; }
}
