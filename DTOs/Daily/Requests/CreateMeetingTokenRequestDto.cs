using System.Text.Json.Serialization;

namespace Backend.DTOs.Daily.Requests;

public sealed class CreateMeetingTokenRequestDto
{
    [JsonPropertyName("properties")]
    public CreateMeetingTokenPropertiesDto Properties { get; set; } = new();
}

public sealed class CreateMeetingTokenPropertiesDto
{
    [JsonPropertyName("room_name")]
    public string RoomName { get; set; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("exp")]
    public long Expiration { get; set; }

    [JsonPropertyName("eject_at_token_exp")]
    public bool EjectAtTokenExp { get; set; } = true;

    [JsonPropertyName("is_owner")]
    public bool IsOwner { get; set; }

    [JsonPropertyName("enable_prejoin_ui")]
    public bool EnablePrejoinUi { get; set; } = true;

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "es";
}
