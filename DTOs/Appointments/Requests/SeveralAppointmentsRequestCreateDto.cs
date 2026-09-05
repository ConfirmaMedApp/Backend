namespace Backend.DTOs.Appointments.Requests;

public class SeveralAppointmentsRequestCreateDto
{
    public List<string> Dates { get; set; } = new();
    public string StartHour { get; set; } = string.Empty;
    public string EndHour { get; set; } = string.Empty;
    public int DurationId { get; set; }
    public int DoctorId { get; set; }
    public int SpecialityId { get; set; }
}
