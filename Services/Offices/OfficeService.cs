using AutoMapper;
using Backend.DTOs.Offices.Requests;
using Backend.DTOs.Offices.Responses;
using Backend.Entities.Offices;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Repositories.Offices;
using FluentValidation;

namespace Backend.Services.Offices;

public class OfficeService(IOfficeRepository officeRepository, IMapper mapper, IValidator<OfficeRequestCreateDto> createValidatorDto, IValidator<OfficeRequestUpdateDto> updateValidatorDto) : IOfficeService
{
    public async Task<IEnumerable<OfficeResponseDto>> GetAllAsync()
    {
        var offices = await officeRepository.GetAllAsync();
        return mapper.Map<IEnumerable<OfficeResponseDto>>(offices);
    }

    public async Task<OfficeResponseDto> GetByIdAsync(int id)
    {
        var office = await officeRepository.GetByIdAsync(id);
        return office == null ? throw new NotFoundException("Consultorio no encontrado") : mapper.Map<OfficeResponseDto>(office);
    }

    public Task<OfficeResponseDto> GetByUserIdAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public async Task<OfficeResponseDto> CreateAsync(OfficeRequestCreateDto dto)
    {
        var validationResult = await createValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var officeEntity = mapper.Map<Office>(dto);
        var createdOffice =  await officeRepository.CreateAsync(officeEntity);
        return createdOffice is null ? throw new BadRequestException("No se ha podido encontrar el consultorio") : await GetByIdAsync(createdOffice.Id);
    }

    public async Task<OfficeResponseDto> UpdateAsync(OfficeRequestUpdateDto dto)
    {
        var validationResult = await updateValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var officeEntity = mapper.Map<Office>(dto);
        var updatedOffice = await officeRepository.UpdateAsync(officeEntity);
        return updatedOffice is null ? throw new NotFoundException("Consultorio no encontrado") : await GetByIdAsync(updatedOffice.Id);
    }
}