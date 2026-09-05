using Backend.DTOs.Appointments.Responses;

namespace Backend.DTOs.AppointmentsAnnexes.Responses;

public class AppointmentAnnexResponseDto
{
    public int Id { get; set; }
    public string File { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AppointmentMinimalDto Appointment { get; set; } = new();
}