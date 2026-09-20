namespace Backend.DTOs.Appointments.Responses;

public class AppointmentVideoProvisionResultDto
{
    public string RoomUrl { get; set; } = string.Empty;
    public string PatientLink { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
