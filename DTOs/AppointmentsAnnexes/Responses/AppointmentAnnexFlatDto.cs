namespace Backend.DTOs.AppointmentsAnnexes.Responses;

public class AppointmentAnnexFlatDto
{
    public int Id { get; set; }
    public string File { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AppointmentId { get; set; }
    public string AppointmentDateAppointment { get; set; } = string.Empty;
    public string AppointmentStartHour { get; set; } = string.Empty;
}
