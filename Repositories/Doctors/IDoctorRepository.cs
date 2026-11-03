using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.Specialities.Responses;
using Backend.Entities.Doctors;

namespace Backend.Repositories.Doctors;

public interface IDoctorRepository
{
    Task<IEnumerable<DoctorFlatDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "");
    Task<DoctorFlatDto?> GetByIdAsync(int id);
    Task<DoctorFlatDto?> CreateAsync(Doctor doctor);
    Task<DoctorFlatDto?> UpdateAsync(Doctor doctor);
    Task<DoctorFlatDto?> UpdatePhotoAsync(int id, string? photoUrl);
    Task<IEnumerable<SpecialityResponseDto>> GetSpecialitiesByDoctorAsync(int doctorId);
    Task<bool> IsDocumentAndDocumentTypeCombinationUnique(string document, int documentTypeId, int? excludedId = null);
}