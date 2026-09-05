using Backend.DTOs.Specialities.Requests;
using Backend.Repositories.Specialities;
using FluentValidation;

namespace Backend.Validators.Specialities;

public class SpecialityRequestUpdateDtoValidator : AbstractValidator<SpecialityRequestUpdatedDto>
{
    public SpecialityRequestUpdateDtoValidator(ISpecialityRepository specialityRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no debe exceder los 100 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no debe exceder los 500 caracteres");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("El estado es obligatorio");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es requerido")
            .MaximumLength(5).WithMessage("El código no debe exceder los 5 caracteres")
            .MustAsync(async (dto, code, cancellationToken) =>
            {
                return !await specialityRepository.IsAlreadyUsedCode(code, dto.Id);

            }).WithMessage("El código debe ser único");
    }
}
