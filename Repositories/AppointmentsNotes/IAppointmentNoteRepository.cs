using Backend.DTOs.AppointmentsNotes.Responses;
using Backend.Entities.AppointmentsNotes;

namespace Backend.Repositories.AppointmentsNotes;

public interface IAppointmentNoteRepository
{
    Task<IEnumerable<AppointmentNoteFlatDto>> GetAllAsync(int appointmentId);
    Task<AppointmentNoteFlatDto?> GetByIdAsync(int id);
    Task<IEnumerable<AppointmentNoteFlatDto>> GetAllByPatientAsync(int patientId);
    Task<AppointmentNoteFlatDto?> CreateAsync(AppointmentNote appointmentNote);
}