using Backend.DTOs.Appointments.Responses;
using Backend.DTOs.AppointmentsAnnexes.Responses;
using Backend.Entities.AppointmentsAnnexes;
using Backend.Persistence;
using Backend.Repositories.Appointments;
using Dapper;

namespace Backend.Repositories.AppointmentsAnnexes;

public class AppointmentAnnexRepository(IDbConnectionFactory dbConnectionFactory, IAppointmentRepository appointmentRepository) : RepositoryGuard, IAppointmentAnnexRepository
{
    public Task<AppointmentFlatDto?> CreateAsync(AppointmentAnnex appointmentAnnex)
    {
        const string command = "SELECT * FROM create_appointment_annex(@File, @Name, @AppointmentId);";
        return ExecuteSafeAsync(async conn =>
        {
            var createAppointmentAnnex = await conn.ExecuteScalarAsync<int>(command, new { appointmentAnnex.File, appointmentAnnex.Name, appointmentAnnex.AppointmentId });
            return await appointmentRepository.GetByIdAsync(createAppointmentAnnex);
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<AppointmentAnnexFlatDto>> GetAllAsync(int appointmentId)
    {
        const string query = "SELECT * FROM get_all_appointments_annexes(@AppointmentId);";
        return ExecuteSafeAsync(async conn => await conn.QueryAsync<AppointmentAnnexFlatDto>(query, new { AppointmentId = appointmentId }), dbConnectionFactory);
    }
}