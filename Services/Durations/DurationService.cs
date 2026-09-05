using AutoMapper;
using Backend.DTOs.Durations.Responses;
using Backend.Repositories.Durations;

namespace Backend.Services.Durations;

public class DurationService(IDurationRepository durationRepository, IMapper mapper) : IDurationService
{
    public async Task<IEnumerable<DurationResponseDto>> GetAllAsync()
    {
        var durations = await durationRepository.GetAllAsync();
        return mapper.Map<IEnumerable<DurationResponseDto>>(durations);
    }
}
