using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.DoctorsHasSpecialities.Requests;
using Backend.DTOs.DoctorsHasSpecialities.Responses;

namespace Backend.Services.DoctorsHasSpecialities;

public interface IDoctorHasSpecialityService
{
    Task<IEnumerable<DoctorResponseDto>> CreateAsync(DoctorSpecialityRequestCreateUpdateDto dto);
    Task<IEnumerable<DoctorResponseDto>> GetBySpecialityIdAsync(int specialityId);

    Task<DoctorSpecialityPriceResponseDto> SetPriceAsync(DoctorSpecialityPriceRequestDto dto);
    Task<IEnumerable<DoctorSpecialityPriceResponseDto>> GetPricesAsync(int? doctorId, int? specialityId);
}
