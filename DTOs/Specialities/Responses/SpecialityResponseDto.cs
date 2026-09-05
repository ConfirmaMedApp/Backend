namespace Backend.DTOs.Specialities.Responses;

public class SpecialityResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool Status { get; set; }
}
