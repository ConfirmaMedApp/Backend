using Backend.DTOs.AppointmentsNotes.Responses;
using Backend.Entities.AppointmentsNotes;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.AppointmentsNotes;

public class AppointmentNoteRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IAppointmentNoteRepository
{
    public Task<AppointmentNoteFlatDto?> CreateAsync(AppointmentNote appointmentNote)
    {
        const string command = "SELECT * FROM create_appointment_note(@Note, @AppointmentId, @UserId);";
        return ExecuteSafeAsync(async conn =>
        {
            var createdId = await conn.ExecuteScalarAsync<int>(command, new { appointmentNote.Note, appointmentNote.AppointmentId, appointmentNote.UserId });
            return await GetByIdAsync(createdId);
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<AppointmentNoteFlatDto>> GetAllAsync(int appointmentId)
    {
        const string query = "SELECT * FROM get_appointment_notes_by_appointment(@AppointmentId);";
        return ExecuteSafeAsync(async conn => await conn.QueryAsync<AppointmentNoteFlatDto>(query, new { AppointmentId = appointmentId }), dbConnectionFactory);
    }

    public Task<IEnumerable<AppointmentNoteFlatDto>> GetAllByPatientAsync(int patientId)
    {
        const string query = "SELECT * FROM get_appointments_notes_by_patient(@PatientId);";
        return ExecuteSafeAsync(async conn => await conn.QueryAsync<AppointmentNoteFlatDto>(query, new { PatientId = patientId }), dbConnectionFactory);
    }

    public Task<AppointmentNoteFlatDto?> GetByIdAsync(int id)
    {
        const string query = "SELECT * FROM get_appointment_note_by_id(@Id);";
        return ExecuteSafeAsync(async conn => await conn.QueryFirstOrDefaultAsync<AppointmentNoteFlatDto>(query, new { Id = id }), dbConnectionFactory);
    }
}
