namespace Backend.DTOs.AppointmentsNotes.Responses;

public class AppointmentNoteFlatDto
{
    public int Id { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserLastname { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public int AppointmentId { get; set; }
    public string AppointmentDateAppointment { get; set; } = string.Empty;
    public string AppointmentStartHour { get; set; } = string.Empty;
}