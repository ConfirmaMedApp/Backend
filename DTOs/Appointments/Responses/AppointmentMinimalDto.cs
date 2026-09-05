namespace Backend.DTOs.Appointments.Responses;

public class AppointmentMinimalDto
{
    public int Id { get; set; }
    public string DateAppointment { get; set; } = string.Empty;
    public string StartHour { get; set; } = string.Empty;
}