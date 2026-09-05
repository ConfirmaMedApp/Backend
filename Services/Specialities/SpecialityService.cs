using AutoMapper;
using Backend.DTOs.Specialities.Requests;
using Backend.DTOs.Specialities.Responses;
using Backend.Entities.Specialities;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Repositories.Specialities;
using FluentValidation;

namespace Backend.Services.Specialities;

public class SpecialityService(ISpecialityRepository specialityRepository, IMapper mapper, IValidator<SpecialityRequestCreateDto> createValidatorDto, IValidator<SpecialityRequestUpdatedDto> updateValidatorDto) : ISpecialityService
{
    public async Task<SpecialityResponseDto?> CreateAsync(SpecialityRequestCreateDto dto)
    {
        var validationResult = await createValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var speciality = mapper.Map<Speciality>(dto);
        var createdSpeciality = await specialityRepository.CreateAsync(speciality);
        return createdSpeciality is null ? throw new BadRequestException("No se ha podido crear la especialidad") 
            : mapper.Map<SpecialityResponseDto?>(createdSpeciality);
    }

    public async Task<IEnumerable<SpecialityResponseDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "")
    {
        var specialities = await specialityRepository.GetAllAsync(limit, offset, status, search);
        return mapper.Map<IEnumerable<SpecialityResponseDto>>(specialities);
    }

    public async Task<SpecialityResponseDto?> GetByIdAsync(int id)
    {
        var speciality = await specialityRepository.GetByIdAsync(id);
        return speciality is null ? throw new NotFoundException("Especialidad no encontrada") : mapper.Map<SpecialityResponseDto?>(speciality);
    }

    public async Task<SpecialityResponseDto?> UpdateAsync(SpecialityRequestUpdatedDto dto)
    {
        var validationResult = await updateValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var speciality = mapper.Map<Speciality>(dto);
        var updatedSpeciality = await specialityRepository.UpdateAsync(speciality);
        return updatedSpeciality is null ? throw new NotFoundException("Especialidad no encontrada") 
            : mapper.Map<SpecialityResponseDto?>(updatedSpeciality);
    }
}
