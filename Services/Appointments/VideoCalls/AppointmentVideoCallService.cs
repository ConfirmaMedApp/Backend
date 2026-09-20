using Backend.DTOs.Appointments.Responses;
using Backend.Entities.Daily;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Repositories.Appointments;
using Backend.Repositories.VideoCalls;
using Backend.Services.Daily;
using Microsoft.Extensions.Options;

namespace Backend.Services.Appointments.VideoCalls;

public class AppointmentVideoCallService(
    IAppointmentRepository appointmentRepository,
    IVideoCallUsageRepository usageRepository,
    IDailyApiService dailyApi,
    IOptions<DailySettings> settings,
    ILogger<AppointmentVideoCallService> logger
) : IAppointmentVideoCallService
{
    private static readonly TimeSpan ColombiaOffset = TimeSpan.FromHours(-5);
    private readonly DailySettings _settings = settings.Value;

    public async Task<AppointmentVideoProvisionResultDto> ProvisionForAsync(
        int appointmentId,
        string patientDisplayName,
        CancellationToken ct = default)
    {
        var context = await appointmentRepository.GetVideoContextAsync(appointmentId)
            ?? throw new NotFoundException("Cita no encontrada");

        var duration = context.EndHour - context.StartHour;
        if (duration <= TimeSpan.Zero)
            throw new BadRequestException("La duración de la cita es inválida");

        if (duration.TotalMinutes > _settings.MaxCallDurationMinutes)
            throw new BadRequestException(
                $"La duración de la cita ({duration.TotalMinutes:0} min) supera el máximo permitido ({_settings.MaxCallDurationMinutes} min) para videollamadas");

        var nbf = ToColombia(context.DateAppointment, context.StartHour);
        var exp = ToColombia(context.DateAppointment, context.EndHour)
            .AddMinutes(_settings.ExpirationGraceMinutes);

        // Idempotencia: si la cita ya tiene una room, reutilizarla y sólo regenerar el token del paciente
        if (!string.IsNullOrWhiteSpace(context.RoomName) && !string.IsNullOrWhiteSpace(context.RoomUrl))
        {
            var reusedToken = await dailyApi.CreateMeetingTokenAsync(
                context.RoomName, patientDisplayName, exp, isOwner: false, ct);
            return BuildResult(context.RoomUrl, reusedToken, exp);
        }

        var reserved = await usageRepository.TryReserveMonthlySlotAsync(
            nbf.Year, nbf.Month, _settings.MonthlyRoomsLimit);
        if (!reserved)
            throw new BadRequestException("Se alcanzó el límite mensual de videollamadas");

        string roomName;
        string roomUrl;
        try
        {
            var room = await dailyApi.CreateRoomAsync(
                name: $"cita-{appointmentId}",
                notBefore: nbf,
                expiration: exp,
                maxParticipants: 2,
                maxDurationSeconds: (int)duration.TotalSeconds,
                ct: ct);
            roomName = room.Name;
            roomUrl = room.Url;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Fallo al crear room en Daily para cita {AppointmentId}; liberando cupo mensual",
                appointmentId);
            await usageRepository.ReleaseMonthlySlotAsync(nbf.Year, nbf.Month);
            throw;
        }

        await appointmentRepository.UpdateVideoRoomAsync(appointmentId, roomName, roomUrl);

        var patientToken = await dailyApi.CreateMeetingTokenAsync(
            roomName, patientDisplayName, exp, isOwner: false, ct);

        return BuildResult(roomUrl, patientToken, exp);
    }

    public async Task<DoctorMeetingTokenResultDto> IssueDoctorTokenAsync(
        int appointmentId,
        string doctorDisplayName,
        CancellationToken ct = default)
    {
        var context = await appointmentRepository.GetVideoContextAsync(appointmentId)
            ?? throw new NotFoundException("Cita no encontrada");

        if (string.IsNullOrWhiteSpace(context.RoomName) || string.IsNullOrWhiteSpace(context.RoomUrl))
            throw new BadRequestException("La sala de videollamada aún no ha sido provisionada");

        var exp = ToColombia(context.DateAppointment, context.EndHour)
            .AddMinutes(_settings.ExpirationGraceMinutes);

        var token = await dailyApi.CreateMeetingTokenAsync(
            context.RoomName!, doctorDisplayName, exp, isOwner: true, ct);

        return new DoctorMeetingTokenResultDto
        {
            RoomUrl = context.RoomUrl,
            Token = token,
            JoinUrl = $"{context.RoomUrl}?t={token}",
            ExpiresAt = exp
        };
    }

    public async Task<string?> GetPatientLinkAsync(
        int appointmentId,
        string patientDisplayName,
        CancellationToken ct = default)
    {
        var context = await appointmentRepository.GetVideoContextAsync(appointmentId);

        if (context is null
            || string.IsNullOrWhiteSpace(context.RoomName)
            || string.IsNullOrWhiteSpace(context.RoomUrl))
        {
            return null;
        }

        var exp = ToColombia(context.DateAppointment, context.EndHour)
            .AddMinutes(_settings.ExpirationGraceMinutes);

        var token = await dailyApi.CreateMeetingTokenAsync(
            context.RoomName, patientDisplayName, exp, isOwner: false, ct);

        return $"{context.RoomUrl}?t={token}";
    }

    public async Task DeprovisionAsync(int appointmentId, CancellationToken ct = default)
    {
        var context = await appointmentRepository.GetVideoContextAsync(appointmentId);

        if (context is null || string.IsNullOrWhiteSpace(context.RoomName))
            return;

        await dailyApi.DeleteRoomAsync(context.RoomName, ct);
        await appointmentRepository.ClearVideoRoomAsync(appointmentId);
    }

    private static DateTimeOffset ToColombia(DateOnly date, TimeOnly hour)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(hour), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, ColombiaOffset);
    }

    private static AppointmentVideoProvisionResultDto BuildResult(string roomUrl, string patientToken, DateTimeOffset exp) =>
        new()
        {
            RoomUrl = roomUrl,
            PatientLink = $"{roomUrl}?t={patientToken}",
            ExpiresAt = exp
        };
}
