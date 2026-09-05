using Backend.DTOs.Durations.Responses;

namespace Backend.Services.Durations;

public interface IDurationService
{
    Task<IEnumerable<DurationResponseDto>> GetAllAsync();
}
