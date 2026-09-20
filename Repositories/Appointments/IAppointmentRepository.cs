using Backend.DTOs.Appointments.Responses;

namespace Backend.Repositories.Appointments;

public interface IAppointmentRepository
{
    Task<int> CreateSeveralAppointmentsAsync(string[] dates,
        string startHour,
        string endHour,
        int durationId,
        int doctorId,
        int specialityId,
        int? userId);
    
    Task<bool> VerifyDuplicateAppointmentsAsync(string[] dates,
        string startHour,
        string endHour,
        int doctorId);
    
    Task<AppointmentFlatDto?> RescheduleToSlotAsync(int oldAppointmentId, int newAppointmentId);
    Task<IEnumerable<(DateOnly CalendarDate, string StatusDay, string Color)>> GetOccupationAppointmentsPerMonthAsync(int year, int month, int? doctorId);
    Task<IEnumerable<AppointmentFlatDto>> GetAllAsync(string dateSelected, int? specialityId, int? doctorId, bool? isOccuped, int? limit, int? offset);
    Task<IEnumerable<AppointmentFlatDto>> GetAllByUserAsync(string dateSelected, int userId, int? specialityId, bool? isOccuped, int? limit, int? offset);
    Task<AppointmentFlatDto?> GetByIdAsync(int id);
    Task<AppointmentFlatDto?> AssignAppointmentAsync(int appointmentId, int patientId);
    Task<IEnumerable<AppointmentFlatDto>> GetByPatientNeedAppointmentAsync(int specialityId, string startHour, string dateSelected);
    Task<IEnumerable<AppointmentFlatDto>> GetRecommendationsForPatientAsync(int specialityId, string dateSelected);
    
    Task<IEnumerable<int>> GetAppointmentsForRemindersAsync(int hoursAhead);
    Task MarkReminderAsSentAsync(int appointmentId, int hourWindow);
    
    Task<bool> IsSlotAvailableAsync(int newAppointmentId);
    Task<bool> HasPatientAssignedAsync(int oldAppointmentId);

    Task<IEnumerable<PatientAttendedByDoctorFlatDto>> GetPatientsAttendedByDoctorAsync(
        int doctorId,
        string? startDate,
        string search,
        int? limit,
        int? offset);

    Task UpdateVideoRoomAsync(int appointmentId, string roomName, string roomUrl);
    Task ClearVideoRoomAsync(int appointmentId);

    Task<AppointmentVideoContextDto?> GetVideoContextAsync(int appointmentId);
}
