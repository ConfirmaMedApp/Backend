namespace Backend.DTOs.Appointments.Requests;

public class AppointmentStateChangeRequestDto
{
    /// <summary>Nota opcional que queda en el historial (p. ej. motivo de cancelación).</summary>
    public string? Note { get; set; }
}
