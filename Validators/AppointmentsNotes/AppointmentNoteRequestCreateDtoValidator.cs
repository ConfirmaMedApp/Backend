using Backend.DTOs.AppointmentsNotes.Requests;
using Backend.Repositories.Appointments;
using Backend.Repositories.Users;
using FluentValidation;

namespace Backend.Validators.AppointmentsNotes;

public class AppointmentNoteRequestCreateDtoValidator : AbstractValidator<AppointmentNoteRequestCreateDto>
{
    public AppointmentNoteRequestCreateDtoValidator(IAppointmentRepository appointmentRepository, IUserRepository userRepository)
    {
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Debes escribir una nota relacionada a la cita")
            .MinimumLength(4).WithMessage("Debe tener minímo 4 caractéres");

        RuleFor(x => x.AppointmentId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
                .WithMessage("Ingresa un id de cita valido")
            .MustAsync(async (appointmentId, _) =>
                await appointmentRepository.GetByIdAsync(appointmentId) is not null
            )
                .WithMessage("La cita no existe");

        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
                .WithMessage("Ingresa un id de usuario valido")
            .MustAsync(async (userId, _) =>
                await userRepository.GetByIdAsync(userId) is not null
            )
                .WithMessage("El usuario no existe");
    }
}