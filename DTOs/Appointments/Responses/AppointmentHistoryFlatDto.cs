namespace Backend.DTOs.Appointments.Responses;

// Proyección plana de get_appointment_history (una fila por cambio de estado).
public class AppointmentHistoryFlatDto
{
    public int Id { get; set; }

    public int? FromStateId { get; set; }
    public string? FromStateCode { get; set; }
    public string? FromStateName { get; set; }
    public string? FromStateColor { get; set; }

    public int ToStateId { get; set; }
    public string ToStateCode { get; set; } = string.Empty;
    public string ToStateName { get; set; } = string.Empty;
    public string ToStateColor { get; set; } = string.Empty;

    public string? Reason { get; set; }
    public string? Note { get; set; }

    public int? ChangedById { get; set; }
    public string? ChangedByName { get; set; }
    public string? ChangedByLastname { get; set; }
    public string? ChangedByRole { get; set; }

    public DateTime CreatedAt { get; set; }
}
