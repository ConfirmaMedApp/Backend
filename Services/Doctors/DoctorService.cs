using AutoMapper;
using Backend.DTOs.Doctors.Requests;
using Backend.DTOs.Doctors.Responses;
using Backend.Entities.Doctors;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Repositories.Doctors;
using FluentValidation;

namespace Backend.Services.Doctors;

public class DoctorService(IDoctorRepository doctorRepository, IMapper mapper, IValidator<DoctorRequestCreateDto> createValidatorDto, IValidator<DoctorRequestUpdateDto> updateValidatorDto) : IDoctorService
{
    public async Task<DoctorResponseDto> CreateAsync(DoctorRequestCreateDto dto)
    {
        var validationResult = await createValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var doctorEntity = mapper.Map<Doctor>(dto);
        var createdDoctor = await doctorRepository.CreateAsync(doctorEntity);
        return createdDoctor is null ? throw new BadRequestException("No se pudo crear el doctor") : await GetByIdAsync(createdDoctor.Id);
    }

    public async Task<IEnumerable<DoctorResponseDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "")
    {
        var doctors = await doctorRepository.GetAllAsync(limit, offset, status, search);
        return mapper.Map<IEnumerable<DoctorResponseDto>>(doctors);
    }

    public async Task<DoctorResponseDto> GetByIdAsync(int id)
    {
        var doctor = await doctorRepository.GetByIdAsync(id);
        return doctor == null ? throw new NotFoundException("Doctor no encontrado") : mapper.Map<DoctorResponseDto>(doctor);
    }

    public async Task<DoctorResponseDto> UpdateAsync(DoctorRequestUpdateDto dto)
    {
        var validationResult = await updateValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var doctorEntity = mapper.Map<Doctor>(dto);
        var updatedDoctor = await doctorRepository.UpdateAsync(doctorEntity);
        return updatedDoctor is null ? throw new NotFoundException("Doctor no encontrado") : await GetByIdAsync(updatedDoctor.Id);
    }
}