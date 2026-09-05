namespace Backend.DTOs.Appointments.Requests;

public class AssignAppointmentRequestDto
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
}
