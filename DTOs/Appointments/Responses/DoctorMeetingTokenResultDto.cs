namespace Backend.DTOs.Appointments.Responses;

public class DoctorMeetingTokenResultDto
{
    public string RoomUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string JoinUrl { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
