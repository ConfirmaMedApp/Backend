namespace Backend.DTOs.Appointments.Requests;

public class RescheduleToSlotRequestDto
{
    public int OldAppointmentId { get; set; }
    public int NewAppointmentId { get; set; }

    /// <summary>Nota opcional que queda en el historial del reagendamiento.</summary>
    public string? Note { get; set; }
}