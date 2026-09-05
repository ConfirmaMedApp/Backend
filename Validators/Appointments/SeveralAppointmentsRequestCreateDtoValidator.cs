using Backend.DTOs.Appointments.Requests;
using Backend.Repositories.Appointments;
using FluentValidation;

namespace Backend.Validators.Appointments;

public class SeveralAppointmentsRequestCreateDtoValidator : AbstractValidator<SeveralAppointmentsRequestCreateDto>
{
    public SeveralAppointmentsRequestCreateDtoValidator(IAppointmentRepository appointmentRepository)
    {
        RuleFor(x => x.Dates)
            .NotEmpty().WithMessage("Debe seleccionar al menos una fecha.");

        RuleFor(x => x.StartHour)
            .NotEmpty().WithMessage("La hora de inicio es obligatoria.");

        RuleFor(x => x.EndHour)
            .NotEmpty().WithMessage("La hora de fin es obligatoria.");

        RuleFor(x => x.DoctorId)
            .GreaterThan(0).WithMessage("Debe seleccionar un médico válido.");
        
        RuleFor(x => x)
            .MustAsync(async (dto, _) =>
            {
                var datesArray = dto.Dates.ToArray();
                
                var hasConflict = await appointmentRepository.VerifyDuplicateAppointmentsAsync(
                    datesArray, 
                    dto.StartHour, 
                    dto.EndHour, 
                    dto.DoctorId
                );

                return !hasConflict; 
            })
            .WithMessage("El médico ya tiene una agenda programada en el horario y fechas seleccionadas.")
            .WithName("DoctorId");
    }
}