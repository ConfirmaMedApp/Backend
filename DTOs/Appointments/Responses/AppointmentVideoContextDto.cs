namespace Backend.DTOs.Appointments.Responses;

public class AppointmentVideoContextDto
{
    public DateOnly DateAppointment { get; set; }
    public TimeOnly StartHour { get; set; }
    public TimeOnly EndHour { get; set; }
    public string? RoomName { get; set; }
    public string? RoomUrl { get; set; }
}
