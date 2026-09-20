namespace Backend.Entities.Appointments;

public class Appointment
{
    public int Id { get; set; }
    public DateTime DateAppointment { get; set; }
    public TimeSpan StarHour { get; set; }
    public TimeSpan EndHour { get; set; }
    public int DurationId { get; set; }
    public int DoctorId { get; set; }
    public int SpecialityId { get; set; }
    public int? PatientId { get; set; }
    public bool Status { get; set; }
    public bool IsOccuped { get; set; }
    public bool IsApproved { get; set; }
    public string? RoomName { get; set; }
    public string? RoomUrl { get; set; }
    public DateTime? RoomCreatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
