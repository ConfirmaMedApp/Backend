using AutoMapper;
using Backend.DTOs.Patients.Requests;
using Backend.DTOs.Patients.Responses;
using Backend.Entities.Patients;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Exceptions.Unauthorized;
using Backend.Repositories.Patients;
using Backend.Services.CurrentUser;
using Backend.Services.Users;
using FluentValidation;

namespace Backend.Services.Patients;

public class PatientService(
    IPatientRepository patientRepository,
    IMapper mapper,
    IValidator<PatientRequestCreateDto> createValidatorDto,
    IValidator<PatientRequestUpdateDto> updateValidatorDto,
    ICurrentUserService currentUserService,
    IUserService userService) : IPatientService
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

    public async Task<IEnumerable<PatientResponseDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "")
    {
        var patients = await patientRepository.GetAllAsync(limit, offset, status, search);
        return mapper.Map<IEnumerable<PatientResponseDto>>(patients);
    }

    public async Task<IEnumerable<PatientResponseDto>> GetAttendedByDoctorAsync(string? startDate, string search, int? limit, int? offset)
    {
        var loggedUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("No te encuentras autenticado");

        var loggedUser = await userService.GetByIdAsync(loggedUserId)
            ?? throw new UnauthorizedException("Usuario no encontrado");

        var doctorId = loggedUser.Doctor?.Id
            ?? throw new BadRequestException("El usuario no tiene un doctor asignado");

        var patients = await patientRepository.GetAttendedByDoctorAsync(doctorId, startDate, search ?? string.Empty, limit, offset);
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
