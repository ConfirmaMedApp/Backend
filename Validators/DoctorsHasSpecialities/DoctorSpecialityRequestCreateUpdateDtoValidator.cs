using Backend.DTOs.DoctorsHasSpecialities.Requests;
using Backend.Repositories.DoctorsHasSpecialities;
using FluentValidation;

namespace Backend.Validators.DoctorsHasSpecialities;

public class DoctorSpecialityRequestCreateUpdateDtoValidator : AbstractValidator<DoctorSpecialityRequestCreateUpdateDto>
{
    public DoctorSpecialityRequestCreateUpdateDtoValidator(IDoctorHasSpecialityRepository doctorHasSpecialityRepository)
    {
        RuleFor(x => x.SpecialityId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
                .WithMessage("Ingresa un id de especialida valido")
            .MustAsync(async (specialityId, _) =>
                await doctorHasSpecialityRepository.ExistsSpecialityAsync(specialityId)
            )
                .WithMessage("La especialidad no existe");

        RuleFor(x => x.DoctorIds)
            .Cascade(CascadeMode.Stop)
            .Must(ids => ids is null || ids.Length == 0 || ids.All(id => id > 0))
                .WithMessage("Todos los ids de doctores deben ser válidos")
            .Must(ids => ids is null || ids.Length == 0 || ids.Distinct().Count() == ids.Length)
                .WithMessage("Hay ids duplicados en doctores")
            .MustAsync(async (doctorIds, _) =>
                doctorIds is null || doctorIds.Length == 0 ||
                await doctorHasSpecialityRepository.ExistsManyDoctorsAsync(doctorIds)
            )
                .WithMessage("Uno o más doctores no existen");
            }
}
