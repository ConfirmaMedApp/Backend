namespace Backend.Entities.AppointmentsAnnexes;

public class AppointmentAnnex
{
    public int Id { get; set; }
    public string File { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AppointmentId { get; set; } 
    public TimeSpan CreatedAt { get; set; }
}
