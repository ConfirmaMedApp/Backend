using Backend.DTOs.Offices.Requests;
using Backend.DTOs.Offices.Responses;

namespace Backend.Services.Offices;

public interface IOfficeService
{
    Task<IEnumerable<OfficeResponseDto>> GetAllAsync();
    Task<OfficeResponseDto> GetByIdAsync(int id);
    Task<OfficeResponseDto> GetByUserIdAsync(int userId);
    Task<OfficeResponseDto> CreateAsync(OfficeRequestCreateDto dto);
    Task<OfficeResponseDto> UpdateAsync(OfficeRequestUpdateDto dto);
}