using AutoMapper;
using Backend.DTOs.Patients.Requests;
using Backend.DTOs.Patients.Responses;
using Backend.Entities.Patients;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Repositories.Patients;
using FluentValidation;

namespace Backend.Services.Patients;

public class PatientService(IPatientRepository patientRepository, IMapper mapper, IValidator<PatientRequestCreateDto> createValidatorDto, IValidator<PatientRequestUpdateDto> updateValidatorDto) : IPatientService
{
    public async Task<PatientResponseDto> CreateAsync(PatientRequestCreateDto dto)
    {
        var validationResult = await createValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var patient = mapper.Map<Patient>(dto);
        var createdPatient = await patientRepository.CreateAsync(patient);
        return mapper.Map<PatientResponseDto>(createdPatient);
    }

    public async Task<IEnumerable<PatientResponseDto>> GetAllAsync(int? limit, int? offset, string search = "")
    {
        var patients = await patientRepository.GetAllAsync(limit, offset, search);
        return mapper.Map<IEnumerable<PatientResponseDto>>(patients);
    }

    public async Task<PatientResponseDto?> GetByDocumentAsync(string document)
    {
        var patient = await patientRepository.GetByDocumentAsync(document);
        return patient is null ? throw new NotFoundException("Paciente no encontrado") : mapper.Map<PatientResponseDto>(patient);
    }

    public async Task<PatientResponseDto?> GetByIdAsync(int id)
    {
        var patient = await patientRepository.GetByIdAsync(id);
        return patient is null ? throw new NotFoundException("Paciente no encontrado") : mapper.Map<PatientResponseDto?>(patient);
    }

    public async Task<PatientResponseDto> UpdateAsync(PatientRequestUpdateDto dto)
    {
        var validationResult = await updateValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var patient = mapper.Map<Patient>(dto);
        var createdPatient = await patientRepository.UpdateAsync(patient);
        return mapper.Map<PatientResponseDto>(createdPatient);
    }
}
