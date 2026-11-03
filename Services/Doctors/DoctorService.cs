using AutoMapper;
using Backend.DTOs.Doctors.Requests;
using Backend.DTOs.Doctors.Responses;
using Backend.Entities.Doctors;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Repositories.Doctors;
using Backend.Services.CloudinaryUpload;
using FluentValidation;

namespace Backend.Services.Doctors;

public class DoctorService(
    IDoctorRepository doctorRepository,
    IMapper mapper,
    IValidator<DoctorRequestCreateDto> createValidatorDto,
    IValidator<DoctorRequestUpdateDto> updateValidatorDto,
    IValidator<DoctorPhotoRequestDto> photoValidatorDto,
    ICloudinaryService cloudinaryService) : IDoctorService
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

    public async Task<DoctorProfileResponseDto> GetProfileAsync(int id)
    {
        var doctor = await doctorRepository.GetByIdAsync(id)
                     ?? throw new NotFoundException("Doctor no encontrado");

        var specialities = await doctorRepository.GetSpecialitiesByDoctorAsync(id);

        var profile = mapper.Map<DoctorProfileResponseDto>(doctor);
        profile.Specialities = specialities;
        return profile;
    }

    public async Task<DoctorResponseDto> UpdatePhotoAsync(int id, DoctorPhotoRequestDto dto)
    {
        var validationResult = await photoValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        _ = await doctorRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Doctor no encontrado");

        var photoUrl = await cloudinaryService.UploadImageAsync(dto.File!, "doctors/photos");

        var updatedDoctor = await doctorRepository.UpdatePhotoAsync(id, photoUrl)
                            ?? throw new NotFoundException("Doctor no encontrado");

        return mapper.Map<DoctorResponseDto>(updatedDoctor);
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