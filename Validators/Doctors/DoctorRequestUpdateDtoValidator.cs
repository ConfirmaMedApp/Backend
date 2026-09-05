using Backend.DTOs.Doctors.Requests;
using Backend.Repositories.Doctors;
using FluentValidation;

namespace Backend.Validators.Doctors;

public class DoctorRequestUpdateDtoValidator : AbstractValidator<DoctorRequestUpdateDto>
{
    public DoctorRequestUpdateDtoValidator(IDoctorRepository doctorRepository)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Ingresa un id valido");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(50).WithMessage("El nombre no puede exceder los 50 caracteres");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio")
            .MaximumLength(50).WithMessage("El apellido no puede exceder los 50 caracteres");

        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("El documento es obligatorio")
            .MaximumLength(20).WithMessage("El documento no puede exceder los 20 caracteres");

        RuleFor(x => x.DocumentTypeId)
            .GreaterThan(0).WithMessage("El id del tipo de documento debe ser valido");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electronico es obligatorio")
            .EmailAddress().WithMessage("El correo electronico debe ser valido")
            .MaximumLength(100).WithMessage("El correo electronico no puede exceder os 100 caracteres");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("El estado es obligatorio");

        RuleFor(x => x)
            .MustAsync(async (dto, cancellationToken) =>
            {
                return !await doctorRepository.IsDocumentAndDocumentTypeCombinationUnique(
                    dto.Document,
                    dto.DocumentTypeId,
                    dto.Id
                );
            })
            .WithMessage("La combinación entre el documento y tipo de documento ya fue usada")
            .When(x => !string.IsNullOrEmpty(x.Document) && x.DocumentTypeId > 0);
    }
}
