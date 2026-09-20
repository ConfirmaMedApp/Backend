using System.Net;
using System.Net.Http.Json;
using Backend.DTOs.Daily.Requests;
using Backend.DTOs.Daily.Responses;
using Backend.Exceptions.BadRequest;

namespace Backend.Services.Daily;

public class DailyApiService(HttpClient http, ILogger<DailyApiService> logger) : IDailyApiService
{
    public async Task<DailyRoomResponseDto> CreateRoomAsync(
        string name,
        DateTimeOffset notBefore,
        DateTimeOffset expiration,
        int maxParticipants,
        int maxDurationSeconds,
        CancellationToken ct = default)
    {
        var payload = new CreateRoomRequestDto
        {
            Name = name,
            Properties = new CreateRoomPropertiesDto
            {
                NotBefore = notBefore.ToUnixTimeSeconds(),
                Expiration = expiration.ToUnixTimeSeconds(),
                EjectAfterElapsed = maxDurationSeconds,
                MaxParticipants = maxParticipants
            }
        };

        var response = await http.PostAsJsonAsync("rooms", payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Daily.co CreateRoom falló ({Status}) para {Name}: {Body}", response.StatusCode, name, body);
            throw new BadRequestException($"No se pudo crear la sala de videollamada ({(int)response.StatusCode})");
        }

        return await response.Content.ReadFromJsonAsync<DailyRoomResponseDto>(cancellationToken: ct)
            ?? throw new BadRequestException("Respuesta vacía al crear la sala de videollamada");
    }

    public async Task<string> CreateMeetingTokenAsync(
        string roomName,
        string userName,
        DateTimeOffset expiration,
        bool isOwner,
        CancellationToken ct = default)
    {
        var payload = new CreateMeetingTokenRequestDto
        {
            Properties = new CreateMeetingTokenPropertiesDto
            {
                RoomName = roomName,
                UserName = userName,
                Expiration = expiration.ToUnixTimeSeconds(),
                IsOwner = isOwner
            }
        };

        var response = await http.PostAsJsonAsync("meeting-tokens", payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Daily.co CreateMeetingToken falló ({Status}) para {Room}: {Body}", response.StatusCode, roomName, body);
            throw new BadRequestException($"No se pudo generar el token de acceso ({(int)response.StatusCode})");
        }

        var result = await response.Content.ReadFromJsonAsync<DailyMeetingTokenResponseDto>(cancellationToken: ct);
        return result?.Token ?? throw new BadRequestException("Respuesta vacía al generar el token de acceso");
    }

    public async Task DeleteRoomAsync(string roomName, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"rooms/{roomName}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("Daily.co DeleteRoom: la room {Room} ya no existía", roomName);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Daily.co DeleteRoom falló ({Status}) para {Room}: {Body}", response.StatusCode, roomName, body);
            throw new BadRequestException($"No se pudo eliminar la sala de videollamada ({(int)response.StatusCode})");
        }
    }
}
