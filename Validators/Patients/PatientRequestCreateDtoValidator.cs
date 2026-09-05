using Backend.DTOs.Patients.Requests;
using Backend.Repositories.Doctors;
using Backend.Repositories.Patients;
using FluentValidation;

namespace Backend.Validators.Patients;

public class PatientRequestCreateDtoValidator : AbstractValidator<PatientRequestCreateDto>
{
    public PatientRequestCreateDtoValidator(IPatientRepository patientRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(50).WithMessage("El nombre no debe exceder los 50 caracteres");

        RuleFor(x => x.Lastname)
            .NotEmpty().WithMessage("El apellido es obligatorio")
            .MaximumLength(50).WithMessage("El apellido no debe exceder los 50 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electronico es obligatorio")
            .EmailAddress().WithMessage("El correo electronico debe ser valido")
            .MaximumLength(100).WithMessage("El correo electronico no debe exceder los 100 caracteres");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("El número de celular es obligatorio")
            .MaximumLength(20).WithMessage("El número de celular no debe exceder los 20 caracteres");

        RuleFor(x => x.Birthdate)
            .NotEmpty().WithMessage("La fecha de nacimiento es obligatoria")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("La fecha de nacimiento debe tener el formato de YYYY-MM-DD");

        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("El documento es obligatorio")
            .MaximumLength(20).WithMessage("El documento no debe exceder los 20 caracteres");

        RuleFor(x => x.DocumentTypeId)
            .GreaterThan(0).WithMessage("El tipo de documento debe ser valido");

        RuleFor(x => x.GenderId)
            .GreaterThan(0).WithMessage("El genero debe ser valido");

        RuleFor(x => x)
           .MustAsync(async (dto, cancellationToken) =>
           {
               return !await patientRepository.DocumentAndDocumentTypeAlreadyExists(
                   dto.Document,
                   dto.DocumentTypeId
               );
           })
           .WithMessage("La combinación de documento y tipo de documento ya fue usada")
           .When(x => !string.IsNullOrEmpty(x.Document) && x.DocumentTypeId > 0);
    }
}
