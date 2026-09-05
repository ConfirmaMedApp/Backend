using Backend.DTOs.Genders.Responses;

namespace Backend.Services.Genders;

public interface IGenderService
{
    Task<IEnumerable<GenderResponseDto>> GetAllAsync();
}
