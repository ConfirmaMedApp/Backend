namespace Backend.DTOs.AppointmentsNotes.Requests;

public class AppointmentNoteRequestCreateDto
{
    public string Note { get; set; } = string.Empty;
    public int AppointmentId { get; set; }
    public int UserId { get; set; }
}