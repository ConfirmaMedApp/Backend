using AutoMapper;
using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.DoctorsHasSpecialities.Requests;
using Backend.DTOs.DoctorsHasSpecialities.Responses;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Repositories.DoctorsHasSpecialities;
using FluentValidation;

namespace Backend.Services.DoctorsHasSpecialities;

public class DoctorHasSpecialityService(
    IDoctorHasSpecialityRepository doctorHasSpecialityRepository,
    IMapper mapper,
    IValidator<DoctorSpecialityRequestCreateUpdateDto> createUpdateValidatorDto,
    IValidator<DoctorSpecialityPriceRequestDto> priceValidatorDto) : IDoctorHasSpecialityService
{
    public async Task<IEnumerable<DoctorResponseDto>> CreateAsync(DoctorSpecialityRequestCreateUpdateDto dto)
    {
        var validationResult = await createUpdateValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var doctors = await doctorHasSpecialityRepository.CreateAsync(dto.SpecialityId, dto.DoctorIds);
        return mapper.Map<IEnumerable<DoctorResponseDto>>(doctors);
    }

    public async Task<IEnumerable<DoctorResponseDto>> GetBySpecialityIdAsync(int specialityId)
    {
        var doctors = await doctorHasSpecialityRepository.GetBySpecialityIdAsync(specialityId);
        return mapper.Map<IEnumerable<DoctorResponseDto>>(doctors);
    }

    public async Task<DoctorSpecialityPriceResponseDto> SetPriceAsync(DoctorSpecialityPriceRequestDto dto)
    {
        var validationResult = await priceValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        return await doctorHasSpecialityRepository.SetPriceAsync(dto.DoctorId, dto.SpecialityId, dto.PriceUsd)
            ?? throw new NotFoundException("No se encontró la relación doctor/especialidad");
    }

    public Task<IEnumerable<DoctorSpecialityPriceResponseDto>> GetPricesAsync(int? doctorId, int? specialityId)
        => doctorHasSpecialityRepository.GetPricesAsync(doctorId, specialityId);
}
