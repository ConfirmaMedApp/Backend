using AutoMapper;
using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.DoctorsHasSpecialities.Requests;
using Backend.Exceptions.BadRequest;
using Backend.Repositories.DoctorsHasSpecialities;
using FluentValidation;

namespace Backend.Services.DoctorsHasSpecialities;

public class DoctorHasSpecialityService(IDoctorHasSpecialityRepository doctorHasSpecialityRepository, IMapper mapper, IValidator<DoctorSpecialityRequestCreateUpdateDto> createUpdateValidatorDto) : IDoctorHasSpecialityService
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
}
