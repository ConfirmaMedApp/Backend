using Backend.DTOs.Users.Responses;

namespace Backend.DTOs.Appointments.Responses;

public class AppointmentHistoryResponseDto
{
    public int Id { get; set; }

    /// <summary>Estado de origen. Es null en el registro inicial de la cita.</summary>
    public AppointmentStateDto? FromState { get; set; }

    public AppointmentStateDto ToState { get; set; } = new AppointmentStateDto();

    /// <summary>Razón del cambio: asignacion, cancelacion, no_asistio, reagendamiento, inicio_automatico, finalizacion_automatica, migracion.</summary>
    public string? Reason { get; set; }

    /// <summary>Nota opcional que acompañó el cambio (p. ej. motivo de cancelación).</summary>
    public string? Note { get; set; }

    /// <summary>Usuario que hizo el cambio. Es null cuando lo hizo el sistema (transiciones automáticas).</summary>
    public UserMinimalDto? ChangedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}
