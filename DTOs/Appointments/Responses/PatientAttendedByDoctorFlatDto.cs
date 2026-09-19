namespace Backend.DTOs.Appointments.Responses;

public class PatientAttendedByDoctorFlatDto
{
    public int AppointmentId { get; set; }
    public string AppointmentDate { get; set; } = string.Empty;
    public string AppointmentStartHour { get; set; } = string.Empty;

    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorLastname { get; set; } = string.Empty;
    public string DoctorDocument { get; set; } = string.Empty;

    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientLastname { get; set; } = string.Empty;
    public string PatientDocument { get; set; } = string.Empty;
}
