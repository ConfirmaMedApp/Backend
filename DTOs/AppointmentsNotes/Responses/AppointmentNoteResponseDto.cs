using Backend.DTOs.Appointments.Responses;
using Backend.DTOs.Users.Responses;

namespace Backend.DTOs.AppointmentsNotes.Responses;

public class AppointmentNoteResponseDto
{
    public int Id { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public UserMinimalDto User { get; set; } = new();
    public AppointmentMinimalDto Appointment { get; set; } = new();
}