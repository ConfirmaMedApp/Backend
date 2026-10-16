using Backend.DTOs.Appointments.Requests;
using FluentValidation;

namespace Backend.Validators.Appointments;

public class RescheduleToSlotRequestDtoValidator : AbstractValidator<RescheduleToSlotRequestDto>
{
    public RescheduleToSlotRequestDtoValidator()
    {
        RuleFor(x => x.OldAppointmentId)
            .GreaterThan(0).WithMessage("La cita de origen no es válida.");

        RuleFor(x => x.NewAppointmentId)
            .GreaterThan(0).WithMessage("El turno de destino no es válido.");

        RuleFor(x => x)
            .Must(x => x.OldAppointmentId != x.NewAppointmentId)
            .WithMessage("No puedes reprogramar una cita al mismo turno.");
    }
}
