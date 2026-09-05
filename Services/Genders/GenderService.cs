using AutoMapper;
using Backend.DTOs.Genders.Responses;
using Backend.Repositories.Genders;

namespace Backend.Services.Genders;

public class GenderService(IGenderRepository genderRepository, IMapper mapper) : IGenderService
{
    public async Task<IEnumerable<GenderResponseDto>> GetAllAsync()
    {
        var genders = await genderRepository.GetAllAsync();
        return mapper.Map<IEnumerable<GenderResponseDto>>(genders);
    }
}
