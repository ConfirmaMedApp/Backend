using Backend.DTOs.Appointments.Responses;
using Backend.DTOs.AppointmentsAnnexes.Requests;
using Backend.DTOs.AppointmentsAnnexes.Responses;

namespace Backend.Services.AppointmentsAnnexes;

public interface IAppointmentAnnexService
{
    Task<IEnumerable<AppointmentAnnexResponseDto>> GetAllAsync(int appointmentId);
    Task<AppointmentResponseDto> CreateSeveralAsync(AppointmentAnnexRequestCreateDto[] dtos);
}