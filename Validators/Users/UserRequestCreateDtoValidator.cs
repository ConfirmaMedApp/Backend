using Backend.DTOs.Users.Requests;
using Backend.Repositories.Users;
using FluentValidation;

namespace Backend.Validators.Users;

public class UserRequestCreateDtoValidator : AbstractValidator<UserRequestCreateDto>
{
    public UserRequestCreateDtoValidator(IUserRepository userRepository)
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
            .MustAsync(async (username, cancellationToken) =>
            {
                return !await userRepository.IsUsernameAlreadyUsed(username);
            })
            .WithMessage("El nombre de usuario debe ser único");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña debe ser obligatorio")
            .MinimumLength(8).WithMessage("La contraseña debe tener minímo 8 caracteres")
            .MaximumLength(100).WithMessage("La contrasña no debe exceder los 100 caracteres");

        RuleFor(x => x.DoctorId)
            .GreaterThan(0).WithMessage("El id de doctor debe ser valido");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("El estado es obligatorio");
    }
}
