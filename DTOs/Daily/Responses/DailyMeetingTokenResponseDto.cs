using System.Text.Json.Serialization;

namespace Backend.DTOs.Daily.Responses;

public sealed class DailyMeetingTokenResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
