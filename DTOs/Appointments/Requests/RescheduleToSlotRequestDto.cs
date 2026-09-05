namespace Backend.DTOs.Appointments.Requests;

public class RescheduleToSlotRequestDto
{
    public int OldAppointmentId { get; set; }
    public int NewAppointmentId { get; set; }
}