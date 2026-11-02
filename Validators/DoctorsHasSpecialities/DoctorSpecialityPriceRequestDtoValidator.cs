using Backend.DTOs.DoctorsHasSpecialities.Requests;
using Backend.Repositories.DoctorsHasSpecialities;
using FluentValidation;

namespace Backend.Validators.DoctorsHasSpecialities;

public class DoctorSpecialityPriceRequestDtoValidator : AbstractValidator<DoctorSpecialityPriceRequestDto>
{
    public DoctorSpecialityPriceRequestDtoValidator(IDoctorHasSpecialityRepository doctorHasSpecialityRepository)
    {
        RuleFor(x => x.DoctorId)
            .GreaterThan(0).WithMessage("Ingresa un id de doctor válido");

        RuleFor(x => x.SpecialityId)
            .GreaterThan(0).WithMessage("Ingresa un id de especialidad válido");

        RuleFor(x => x.PriceUsd)
            .GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo");

        // El precio solo se fija sobre una relación doctor↔especialidad ya existente.
        RuleFor(x => x)
            .MustAsync(async (dto, _) =>
                await doctorHasSpecialityRepository.ExistsRelationAsync(dto.DoctorId, dto.SpecialityId))
            .WithMessage("El doctor no tiene asignada esa especialidad. Asígnala primero.")
            .WithName("DoctorId");
    }
}
