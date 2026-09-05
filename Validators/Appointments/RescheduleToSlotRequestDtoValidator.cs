using Backend.DTOs.Appointments.Requests;
using Backend.Repositories.Appointments;
using FluentValidation;

namespace Backend.Validators.Appointments;

public class RescheduleToSlotRequestDtoValidator : AbstractValidator<RescheduleToSlotRequestDto>
{
    public  RescheduleToSlotRequestDtoValidator(IAppointmentRepository appointmentRepository)
    {
        RuleFor(x => x.OldAppointmentId)
            .MustAsync(async (id, cancellation) => 
                await appointmentRepository.HasPatientAssignedAsync(id))
            .WithMessage("La cita de origen no tiene un paciente asignado.");

        RuleFor(x => x.NewAppointmentId)
            .MustAsync(async (id, cancellation) => 
                await appointmentRepository.IsSlotAvailableAsync(id))
            .WithMessage("El turno de destino ya no está disponible.");
            
        RuleFor(x => x)
            .Must(x => x.OldAppointmentId != x.NewAppointmentId)
            .WithMessage("No puedes reprogramar una cita al mismo horario.");
    }
}