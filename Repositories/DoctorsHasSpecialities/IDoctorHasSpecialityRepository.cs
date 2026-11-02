using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.DoctorsHasSpecialities.Responses;
using Backend.Entities.DoctorsHasSpecialities;

namespace Backend.Repositories.DoctorsHasSpecialities;

public interface IDoctorHasSpecialityRepository
{
    Task<IEnumerable<DoctorFlatDto>> CreateAsync(int specialityId, int[]? doctorIds);
    Task<IEnumerable<DoctorFlatDto>> GetBySpecialityIdAsync(int specialityId);
    Task<bool> ExistsSpecialityAsync(int specialityId);
    Task<bool> ExistsManyDoctorsAsync(int[] doctorIds);

    // Precios por combinación doctor + especialidad.
    Task<bool> ExistsRelationAsync(int doctorId, int specialityId);
    Task<DoctorSpecialityPriceResponseDto?> SetPriceAsync(int doctorId, int specialityId, decimal priceUsd);
    Task<IEnumerable<DoctorSpecialityPriceResponseDto>> GetPricesAsync(int? doctorId, int? specialityId);
}
