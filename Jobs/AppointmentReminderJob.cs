using AutoMapper;
using Backend.DTOs.Appointments.Responses;
using Backend.Repositories.Appointments;
using Backend.Services.Appointments;
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
    IMapper mapper)
{
    public async Task SendPendingRemindersAsync()
    {
        await ProcessWindow(24);
        await ProcessWindow(2);
    }
    
    private async Task ProcessWindow(int hours)
    {
        var pending = await repository.GetAppointmentsForRemindersAsync(hours);

        foreach (var appointment in pending)
        {
            try 
            {
                var patient = await patientService.GetByIdAsync((int)appointment.PatientId!);
                var user = await userService.GetByIdAsync(appointment.UserId);
                var office = await officeService.GetByIdAsync(user!.Office.Id);

                await appointmentService.SendReminderEmailAsync(patient!, office, mapper.Map<AppointmentResponseDto>(appointment));
                
                await repository.MarkReminderAsSentAsync(appointment.Id, hours);
            }
            catch (Exception ex)
            {
                // Loguear error pero continuar con la siguiente cita
            }
        }
    }
}