using Backend.DTOs.Patients.Responses;
using Backend.Entities.Patients;

namespace Backend.Repositories.Patients;

public interface IPatientRepository
{
    Task<IEnumerable<PatientFlatDto>> GetAllAsync(int? limit, int? offset, string search = "");
    Task<IEnumerable<PatientFlatDto>> GetAttendedByDoctorAsync(int doctorId, string? startDate, string search, int? limit, int? offset);
    Task<PatientFlatDto?> GetByIdAsync(int id);
    Task<PatientFlatDto?> GetByDocumentAsync(string document);
    Task<PatientFlatDto?> CreateAsync(Patient patient);
    Task<PatientFlatDto?> UpdateAsync(Patient patient);
    Task<bool> DocumentAndDocumentTypeAlreadyExists(string document, int documentTypeId, int? excludingPatientId = null);
}
