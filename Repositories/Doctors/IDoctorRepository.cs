using Backend.DTOs.Doctors.Responses;
using Backend.Entities.Doctors;

namespace Backend.Repositories.Doctors;

public interface IDoctorRepository
{
    Task<IEnumerable<DoctorFlatDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "");
    Task<DoctorFlatDto?> GetByIdAsync(int id);
    Task<DoctorFlatDto?> CreateAsync(Doctor doctor);
    Task<DoctorFlatDto?> UpdateAsync(Doctor doctor);
    Task<bool> IsDocumentAndDocumentTypeCombinationUnique(string document, int documentTypeId, int? excludedId = null);
}