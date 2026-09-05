using Backend.DTOs.Appointments.Responses;
using Backend.DTOs.AppointmentsAnnexes.Responses;
using Backend.Entities.AppointmentsAnnexes;

namespace Backend.Repositories.AppointmentsAnnexes;

public interface IAppointmentAnnexRepository
{
    Task<IEnumerable<AppointmentAnnexFlatDto>> GetAllAsync(int appointmentId);
    Task<AppointmentFlatDto?> CreateAsync(AppointmentAnnex appointmentAnnex);
}
