using Backend.DTOs.AppointmentsNotes.Requests;
using Backend.DTOs.AppointmentsNotes.Responses;

namespace Backend.Services.AppointmentsNotes;

public interface IAppointmentNoteService
{
    Task<AppointmentNoteResponseDto?> CreateAsync(AppointmentNoteRequestCreateDto dto);
    Task<IEnumerable<AppointmentNoteResponseDto>> GetAllAsync(int diaryId);
    Task<IEnumerable<AppointmentNoteResponseDto>> GetAllByPatientAsync(int patientId);
    Task<AppointmentNoteResponseDto?> GetByIdAsync(int id);
}