using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.Patients.Responses;

namespace Backend.DTOs.Appointments.Responses;

public class PatientAttendedByDoctorResponseDto
{
    public AppointmentMinimalDto Appointment { get; set; } = new();
    public DoctorMinimalDto Doctor { get; set; } = new();
    public PatientMinimalDto Patient { get; set; } = new();
}
