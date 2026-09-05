using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.DoctorsHasSpecialities.Requests;

namespace Backend.Services.DoctorsHasSpecialities;

public interface IDoctorHasSpecialityService
{
    Task<IEnumerable<DoctorResponseDto>> CreateAsync(DoctorSpecialityRequestCreateUpdateDto dto);
    Task<IEnumerable<DoctorResponseDto>> GetBySpecialityIdAsync(int specialityId);
}
