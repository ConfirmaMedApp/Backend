namespace Backend.DTOs.MailerSend;

public class AppointmentConfirmationEmailDto
{
    public string ToName { get; set; } = default!;
    public string OfficeName { get; set; } = default!;
    public string OfficeNit { get; set; } = default!;
    public string OfficeAddress { get; set; } = default!;
    public string OfficeBrandUrl { get; set; } = default!;
    public string DoctorName { get; set; } = default!;
    public string SpecialityName { get; set; } = default!;
    public string DateAppointment { get; set; } = default!;
    public string StartHour { get; set; } = default!;
    public string EndHour { get; set; } = default!;
    public string? VideoCallLink { get; set; }
}
