namespace Backend.DTOs.Bird;

public sealed record BirdEmailRequestDto
(
    BirdEmailAddress from,
    List<BirdEmailAddress> to,
    string subject,
    string html,
    string category
);

public sealed record BirdEmailAddress(string email, string name);
