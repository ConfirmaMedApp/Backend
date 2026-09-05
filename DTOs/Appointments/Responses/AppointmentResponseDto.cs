using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.Durations.Responses;
using Backend.DTOs.Patients.Responses;
using Backend.DTOs.Specialities.Responses;

namespace Backend.DTOs.Appointments.Responses;

public class AppointmentResponseDto
{
    public int Id { get; set; }
    public string DateAppointment { get; set; } = string.Empty;
    public string StartHour { get; set; } = string.Empty;
    public string EndHour { get; set; } = string.Empty;
    public DurationResponseDto Duration { get; set; } = new DurationResponseDto();
    public DoctorMinimalDto Doctor { get; set; } = new DoctorMinimalDto();
    public SpecialityMinimalDto Speciality { get; set; } = new SpecialityMinimalDto();
    public PatientMinimalDto? Patient { get; set; }
    public bool Status { get; set; }
    public bool IsOccuped { get; set; }
    public bool IsApproved { get; set; }
    public int UserId { get; set; }
}
