namespace Backend.DTOs.AppointmentsAnnexes.Requests;

public class AppointmentAnnexRequestCreateDto
{
    public IFormFile File { get; set; } = null!;
    public int AppointmentId { get; set; }
}
