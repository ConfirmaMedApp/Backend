using Backend.DTOs.Users.Requests;
using Backend.Repositories.Users;
using FluentValidation;

namespace Backend.Validators.Users;

public class UserRequestUpdateDtoValidator : AbstractValidator<UserRequestUpdateDto>
{
    public UserRequestUpdateDtoValidator(IUserRepository userRepository)
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
            .MaximumLength(100).WithMessage("El correo electronico no debe excede los 100 caracteres");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .MaximumLength(20).WithMessage("El nombre de usuario no debe exceder los 20 caracteres")
            .MustAsync(async (dto, username, cancellationToken) =>
            {
                return !await userRepository.IsUsernameAlreadyUsed(username, dto.Id);
            })
            .WithMessage("El nombre de usuario debe ser único");

        RuleFor(x => x.DoctorId)
            .GreaterThan(0).WithMessage("El id de doctor debe ser valido");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("El estado es obligatorio");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("El rol es obligatorio")
            .Must(role => new[] { "admin", "secretaria", "doctor" }.Contains(role))
            .WithMessage("El rol debe ser admin, secretaria o doctor");
    }
}
