using Backend.DTOs.Daily.Responses;

namespace Backend.Services.Daily;

public interface IDailyApiService
{
    Task<DailyRoomResponseDto> CreateRoomAsync(
        string name,
        DateTimeOffset notBefore,
        DateTimeOffset expiration,
        int maxParticipants,
        int maxDurationSeconds,
        CancellationToken ct = default);

    Task<string> CreateMeetingTokenAsync(
        string roomName,
        string userName,
        DateTimeOffset expiration,
        bool isOwner,
        CancellationToken ct = default);

    Task DeleteRoomAsync(string roomName, CancellationToken ct = default);
}
