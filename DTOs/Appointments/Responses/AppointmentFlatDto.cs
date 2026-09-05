namespace Backend.DTOs.Appointments.Responses;

public class AppointmentFlatDto
{
    public int Id { get; set; }
    public string DateAppointment { get; set; } = string.Empty;
    public string StartHour { get; set; } = string.Empty;
    public string EndHour { get; set; } = string.Empty;
    public int DurationId { get; set; }
    public string DurationInterval { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorLastname { get; set; } = string.Empty;
    public string DoctorDocument { get; set; } = string.Empty;
    public int SpecialityId { get; set; }
    public string SpecialityName { get; set; } = string.Empty;
    public string SpecialityCode { get; set; } = string.Empty;
    public int? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientLastname { get; set; }
    public string? PatientDocument { get; set; }
    public bool Status { get; set; }
    public bool IsOccuped { get; set; }
    public bool IsApproved { get; set; }
    public int UserId { get; set; }
}
