using Backend.DTOs.Appointments.Responses;

namespace Backend.Services.Appointments.VideoCalls;

public interface IAppointmentVideoCallService
{
    Task<AppointmentVideoProvisionResultDto> ProvisionForAsync(
        int appointmentId,
        string patientDisplayName,
        CancellationToken ct = default);

    Task<DoctorMeetingTokenResultDto> IssueDoctorTokenAsync(
        int appointmentId,
        string doctorDisplayName,
        CancellationToken ct = default);

    Task<string?> GetPatientLinkAsync(
        int appointmentId,
        string patientDisplayName,
        CancellationToken ct = default);

    Task DeprovisionAsync(int appointmentId, CancellationToken ct = default);
}
