using Backend.DTOs.Offices.Requests;
using Backend.Repositories.Offices;
using FluentValidation;

namespace Backend.Validators.Offices;

public class OfficeRequestUpdateDtoValidator : AbstractValidator<OfficeRequestUpdateDto>
{
    public OfficeRequestUpdateDtoValidator(IOfficeRepository officeRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no debe exceder los 100 caracteres");

        RuleFor(x => x.Nit)
            .NotEmpty().WithMessage("El NIT es obligatorio")
            .MaximumLength(20).WithMessage("El NIT no debe exceder los 20 caracteres")
            .MustAsync(async (dto, nit, cancellationToken) =>
                    !await officeRepository.IsNitUniqueAsync(nit, dto.Id)
            )
            .WithMessage("El NIT debe ser único");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("El logo es obligatorio");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("La dirección es obligatoria");
    }
}