using Backend.DTOs.Patients.Requests;
using Backend.DTOs.Patients.Responses;

namespace Backend.Services.Patients;

public interface IPatientService
{
    Task<IEnumerable<PatientResponseDto>> GetAllAsync(int? limit, int? offset, string search = "");
    Task<PatientResponseDto?> GetByIdAsync(int id);
    Task<PatientResponseDto?> GetByDocumentAsync(string document);
    Task<PatientResponseDto> CreateAsync(PatientRequestCreateDto dto);
    Task<PatientResponseDto> UpdateAsync(PatientRequestUpdateDto dto);
}
