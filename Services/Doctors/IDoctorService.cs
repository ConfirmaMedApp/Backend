using Backend.DTOs.Doctors.Requests;
using Backend.DTOs.Doctors.Responses;

namespace Backend.Services.Doctors;

public interface IDoctorService
{
    Task<IEnumerable<DoctorResponseDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "");
    Task<DoctorResponseDto> GetByIdAsync(int id);
    Task<DoctorResponseDto> CreateAsync(DoctorRequestCreateDto dto);
    Task<DoctorResponseDto> UpdateAsync(DoctorRequestUpdateDto dto);
}