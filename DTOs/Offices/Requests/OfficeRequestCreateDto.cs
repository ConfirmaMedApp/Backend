namespace Backend.DTOs.Offices.Requests;

public class OfficeRequestCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool Status { get; set; }
}