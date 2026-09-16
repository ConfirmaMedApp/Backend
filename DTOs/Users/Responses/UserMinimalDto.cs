namespace Backend.DTOs.Users.Responses;

public class UserMinimalDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}