using Backend.Repositories.Appointments;
using Backend.Services.Appointments;
using Backend.Services.Appointments.VideoCalls;
using Backend.Services.Offices;
using Backend.Services.Patients;
using Backend.Services.Users;

namespace Backend.Jobs;

public class AppointmentReminderJob(
    IAppointmentRepository repository,
    IPatientService patientService,
    IOfficeService officeService,
    IUserService userService,
    IAppointmentService appointmentService,
    IAppointmentVideoCallService videoCallService,
    ILogger<AppointmentReminderJob> logger)
{
    public async Task SendPendingRemindersAsync()
    {
        await ProcessWindow(24);
        await ProcessWindow(2);
    }

    private async Task ProcessWindow(int hours)
    {
        var pendingIds = await repository.GetAppointmentsForRemindersAsync(hours);

        foreach (var appointmentId in pendingIds)
        {
            try
            {
                var appointment = await appointmentService.GetByIdAsync(appointmentId);

                if (appointment.Patient is null)
                {
                    logger.LogWarning("Recordatorio omitido para cita {AppointmentId}: sin paciente asignado", appointmentId);
                    continue;
                }

                var patient = await patientService.GetByIdAsync(appointment.Patient.Id);
                if (patient is null)
                {
                    logger.LogWarning("Recordatorio omitido para cita {AppointmentId}: paciente {PatientId} no encontrado", appointmentId, appointment.Patient.Id);
                    continue;
                }

                var user = await userService.GetByIdAsync(appointment.UserId);
                var office = await officeService.GetByIdAsync(user!.Office.Id);

                string? videoLink = null;
                try
                {
                    videoLink = await videoCallService.GetPatientLinkAsync(
                        appointmentId,
                        $"{patient.Name} {patient.Lastname}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "No se pudo regenerar el link de videollamada para el recordatorio de la cita {AppointmentId}; se envía sin link",
                        appointmentId);
                }

                await appointmentService.SendReminderEmailAsync(patient, office, appointment, videoLink);
                await repository.MarkReminderAsSentAsync(appointmentId, hours);

                logger.LogInformation("Recordatorio {Window}h enviado para cita {AppointmentId}", hours, appointmentId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Fallo enviando recordatorio {Window}h para cita {AppointmentId}",
                    hours, appointmentId);
            }
        }
    }
}
