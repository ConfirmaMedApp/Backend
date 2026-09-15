using Backend.DTOs.Appointments.Requests;
using Backend.DTOs.Appointments.Responses;
using Backend.DTOs.Offices.Responses;
using Backend.DTOs.Patients.Responses;

namespace Backend.Services.Appointments;

public interface IAppointmentService
{
    Task<int> CreateSeveralAppointmentsAsync(SeveralAppointmentsRequestCreateDto dto);
    Task<AppointmentResponseDto> RescheduleToSlotAsync (RescheduleToSlotRequestDto dto);
    Task<IEnumerable<OccupationAppointmentsPerMonthResponseDto>> GetOccupationAppointmentsPerMonthAsync(int year, int month, int? doctorId);
    Task<IEnumerable<AppointmentResponseDto>> GetAllAsync(string dateSelected, int? specialityId, int? doctorId, bool? isOccuped, int? limit, int? offset);
    Task<IEnumerable<AppointmentResponseDto>> GetAllByUserAsync(string dateSelected, int? specialityId, bool? isOccuped, int? limit, int? offset);
    Task<AppointmentResponseDto> AssignAppointmentAsync(AssignAppointmentRequestDto dto);
    Task<AppointmentResponseDto> GetByIdAsync(int id);
    Task<IEnumerable<AppointmentResponseDto>> GetByPatientNeedAppointmentAsync(int specialityId, string startHour, string dateSelected);
    Task<IEnumerable<AppointmentResponseDto>> GetRecommendationsForPatientAsync(int specialityId, string dateSelected);
    Task SendConfirmationEmailAsync(PatientResponseDto patientResponseDto, OfficeResponseDto officeResponseDto, AppointmentResponseDto appointmentResponseDto);

    Task SendReminderEmailAsync(PatientResponseDto patientResponseDto, OfficeResponseDto officeResponseDto,
        AppointmentResponseDto appointmentResponseDto);
}
