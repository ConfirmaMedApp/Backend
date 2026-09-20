using System.Text.Json.Serialization;

namespace Backend.DTOs.Daily.Requests;

public sealed class CreateRoomRequestDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("privacy")]
    public string Privacy { get; set; } = "private";

    [JsonPropertyName("properties")]
    public CreateRoomPropertiesDto Properties { get; set; } = new();
}

public sealed class CreateRoomPropertiesDto
{
    [JsonPropertyName("nbf")]
    public long NotBefore { get; set; }

    [JsonPropertyName("exp")]
    public long Expiration { get; set; }

    [JsonPropertyName("eject_at_room_exp")]
    public bool EjectAtRoomExp { get; set; } = true;

    [JsonPropertyName("eject_after_elapsed")]
    public int EjectAfterElapsed { get; set; }

    [JsonPropertyName("max_participants")]
    public int MaxParticipants { get; set; } = 2;

    [JsonPropertyName("enable_prejoin_ui")]
    public bool EnablePrejoinUi { get; set; } = true;

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "es";
}
