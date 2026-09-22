using Backend.DTOs.Appointments.Responses;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.Appointments;

public class AppointmentRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IAppointmentRepository
{
    public Task<AppointmentFlatDto?> AssignAppointmentAsync(int appointmentId, int patientId)
    {
        const string command = "SELECT * FROM assignment_patient(@AppointmentId, @PatientId);";

        return ExecuteSafeAsync(async conn =>
        {
            var appointment = await conn.ExecuteScalarAsync<int>(command, new
            {
                AppointmentId = appointmentId,
                PatientId = patientId
            });

            return await GetByIdAsync(appointment);

        }, dbConnectionFactory);
    }

    public Task<int> CreateSeveralAppointmentsAsync(string[] dates, string startHour, string endHour, int durationId,
        int doctorId, int specialityId, int? userId)
    {
        const string command = "SELECT * FROM create_diary(@Dates::date[], @StartHour::time, @EndHour::time, @DurationId, @DoctorId, @SpecialityId, @UserId);";

        return ExecuteSafeAsync(async conn =>
        {
            var diary = await conn.ExecuteScalarAsync<int>(command, new
            {
                Dates = dates,
                StartHour = startHour,
                EndHour = endHour,
                DurationId = durationId,
                DoctorId = doctorId,
                SpecialityId = specialityId,
                UserId = userId
            });

            return diary;
        }, dbConnectionFactory);
    }

    public Task<bool> VerifyDuplicateAppointmentsAsync(string[] dates, string startHour, string endHour, int doctorId)
    {
        const string query = "SELECT * FROM check_diary_conflict(@Dates::date[], @StartHour::time, @EndHour::time, @DoctorId);";

        return ExecuteSafeAsync(async conn => await conn.ExecuteScalarAsync<bool>(query,
            new { Dates = dates, StartHour = startHour, EndHour = endHour, DoctorId = doctorId }), dbConnectionFactory);
    }

    public Task<AppointmentFlatDto?> RescheduleToSlotAsync(int oldAppointmentId, int newAppointmentId)
    {
        const string command = "SELECT * FROM reschedule_to_slot(@OldAppointmentId, @NewAppointmentId);";

        return ExecuteSafeAsync(async conn =>
        {
            var reschedule = await conn.ExecuteScalarAsync<int>(command, new { OldAppointmentId = oldAppointmentId, NewAppointmentId = newAppointmentId });
            return await GetByIdAsync(reschedule);
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<AppointmentFlatDto>> GetAllAsync(string dateSelected, int? specialityId, int? doctorId, bool? isOccuped, int? limit, int? offset)
    {
        const string query = "SELECT * FROM get_all_appointments(@DateSelected::date, @SpecialityId, @DoctorId, @IsOccuped, @Limit, @Offset);";

        return ExecuteSafeAsync(async conn =>
        {
            var appointments = await conn.QueryAsync<AppointmentFlatDto>(query, new
            {
                DateSelected = dateSelected,
                SpecialityId = specialityId,
                DoctorId = doctorId,
                IsOccuped = isOccuped,
                Limit = limit,
                Offset = offset
            });
            return appointments;
        }, dbConnectionFactory);
    }

    public Task<AppointmentFlatDto?> GetByIdAsync(int id)
    {
        const string query = "SELECT * FROM get_appointment_by_id(@Id);";

        return ExecuteSafeAsync(async conn =>
        {
            var appointment = await conn.QueryFirstOrDefaultAsync<AppointmentFlatDto>(query, new { Id = id });
            return appointment ?? null;
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<AppointmentFlatDto>> GetByPatientNeedAppointmentAsync(int specialityId, string startHour, string dateSelected)
    {
        const string query = "SELECT * FROM get_appointment_by_speciality_hour_date(@SpecialityId, @StartHour::time, @DateSelected::date);";

        return ExecuteSafeAsync(async conn =>
        {
            var appointments = await conn.QueryAsync<AppointmentFlatDto>(query, new
            {
                SpecialityId = specialityId,
                StartHour = startHour,
                DateSelected = dateSelected
            });

            return appointments;
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<(DateOnly CalendarDate, string StatusDay, string Color)>> GetOccupationAppointmentsPerMonthAsync(int year, int month, int? doctorId)
    {
        const string query = "SELECT * FROM get_doctor_calendar_days(@Year, @Month, @DoctorId);";

        return ExecuteSafeAsync(async conn =>
        {
            var days = await conn.QueryAsync<(DateOnly CalendarDate, string StatusDay, string Color)>(query, new
            {
                Year = year,
                Month = month,
                DoctorId = doctorId
            });

            return days;
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<AppointmentFlatDto>> GetRecommendationsForPatientAsync(int specialityId, string dateSelected)
    {
        const string query = "SELECT * FROM get_appointment_recomendations(@SpecialityId, @DateSelected::date);";

        return ExecuteSafeAsync(async conn =>
        {
            var appointments = await conn.QueryAsync<AppointmentFlatDto>(query, new
            {
                SpecialityId = specialityId,
                DateSelected = dateSelected
            });

            return appointments;
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<int>> GetAppointmentsForRemindersAsync(int hoursAhead)
    {
        const string query = """
                             SELECT id
                             FROM appointments
                             WHERE is_occuped = true
                               AND patient_id IS NOT NULL
                               AND date_appointment + start_hour <= (NOW() + (interval '1 hour' * @Hours))
                               AND date_appointment + start_hour > NOW()
                               AND (
                                   (@Hours = 24 AND reminder_24h_sent = false) OR
                                   (@Hours = 2  AND reminder_2h_sent = false)
                               );
                             """;

        return ExecuteSafeAsync(async conn =>
                await conn.QueryAsync<int>(query, new { Hours = hoursAhead }),
            dbConnectionFactory);
    }

    public Task MarkReminderAsSentAsync(int appointmentId, int hourWindow)
    {
        var column = hourWindow == 24 ? "reminder_24h_sent" : "reminder_2h_sent";
        var query = $"UPDATE appointments SET {column} = true WHERE id = @Id";
    
        return ExecuteSafeAsync(async conn => 
                await conn.ExecuteAsync(query, new { Id = appointmentId }), 
            dbConnectionFactory);
    }

    public Task<bool> IsSlotAvailableAsync(int newAppointmentId)
    {
        const string query = "SELECT * FROM is_slot_available(@NewAppointmentId);";
        
        return ExecuteSafeAsync(async conn => await conn.ExecuteScalarAsync<bool>(query, new { NewAppointmentId = newAppointmentId }), dbConnectionFactory);
    }

    public Task<bool> HasPatientAssignedAsync(int oldAppointmentId)
    {
        const string query = "SELECT * FROM has_patient_assigned(@OldAppointmentId);";
        
        return ExecuteSafeAsync(async conn => await conn.ExecuteScalarAsync<bool>(query, new { OldAppointmentId = oldAppointmentId }), dbConnectionFactory);
    }

    public Task<AppointmentVideoContextDto?> GetVideoContextAsync(int appointmentId)
    {
        const string query = "SELECT * FROM get_appointment_video_context(@Id);";

        return ExecuteSafeAsync(async conn =>
                await conn.QueryFirstOrDefaultAsync<AppointmentVideoContextDto>(query, new { Id = appointmentId }),
            dbConnectionFactory);
    }

    public Task UpdateVideoRoomAsync(int appointmentId, string roomName, string roomUrl)
    {
        const string command = "SELECT update_appointment_video_room(@AppointmentId, @RoomName, @RoomUrl);";

        return ExecuteSafeAsync(async conn =>
                await conn.ExecuteAsync(command, new
                {
                    AppointmentId = appointmentId,
                    RoomName = roomName,
                    RoomUrl = roomUrl
                }),
            dbConnectionFactory);
    }

    public Task ClearVideoRoomAsync(int appointmentId)
    {
        const string command = "SELECT clear_appointment_video_room(@AppointmentId);";

        return ExecuteSafeAsync(async conn =>
                await conn.ExecuteAsync(command, new { AppointmentId = appointmentId }),
            dbConnectionFactory);
    }

    public Task<IEnumerable<AppointmentFlatDto>> GetAllByPatientAsync(int patientId, int? specialityId, string? startDate, int? limit, int? offset)
    {
        const string query = "SELECT * FROM get_all_appointments_by_patient(@PatientId, @SpecialityId, @StartDate::date, @Limit, @Offset);";

        return ExecuteSafeAsync(async conn =>
        {
            var appointments = await conn.QueryAsync<AppointmentFlatDto>(query, new
            {
                PatientId = patientId,
                SpecialityId = specialityId,
                StartDate = startDate,
                Limit = limit,
                Offset = offset
            });
            return appointments;
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<AppointmentFlatDto>> GetAllByUserAsync(string dateSelected, int userId, int? specialityId, bool? isOccuped, int? limit, int? offset)
    {
        const string query = "SELECT * FROM get_all_appointments_by_user(@DateSelected::date, @UserId, @SpecialityId, @IsOccuped, @Limit, @Offset);";

        return ExecuteSafeAsync(async conn =>
        {
            var appointments = await conn.QueryAsync<AppointmentFlatDto>(query, new
            {
                DateSelected = dateSelected,
                UserId = userId,
                SpecialityId = specialityId,
                IsOccuped = isOccuped,
                Limit = limit,
                Offset = offset
            });
            return appointments;
        }, dbConnectionFactory);
    }
}
