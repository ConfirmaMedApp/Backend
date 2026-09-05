using Backend.DTOs.Specialities.Requests;
using Backend.DTOs.Specialities.Responses;

namespace Backend.Services.Specialities;

public interface ISpecialityService
{
    Task<IEnumerable<SpecialityResponseDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "");
    Task<SpecialityResponseDto?> GetByIdAsync(int id);
    Task<SpecialityResponseDto?> CreateAsync(SpecialityRequestCreateDto dto);
    Task<SpecialityResponseDto?> UpdateAsync(SpecialityRequestUpdatedDto dto);
}
