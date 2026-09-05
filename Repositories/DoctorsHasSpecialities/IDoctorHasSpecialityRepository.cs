using Backend.DTOs.Doctors.Responses;
using Backend.Entities.DoctorsHasSpecialities;

namespace Backend.Repositories.DoctorsHasSpecialities;

public interface IDoctorHasSpecialityRepository
{
    Task<IEnumerable<DoctorFlatDto>> CreateAsync(int specialityId, int[]? doctorIds);
    Task<IEnumerable<DoctorFlatDto>> GetBySpecialityIdAsync(int specialityId);
    Task<bool> ExistsSpecialityAsync(int specialityId);
    Task<bool> ExistsManyDoctorsAsync(int[] doctorIds);
}
