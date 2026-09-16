namespace Backend.Entities.AppointmentsNotes;

public class AppointmentNote
{
    public int Id { get; set; }
    public string Note { get; set; } = string.Empty;
    public int AppointmentId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}