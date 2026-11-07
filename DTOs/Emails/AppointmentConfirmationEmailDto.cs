namespace Backend.DTOs.Emails;

public class AppointmentConfirmationEmailDto
{
    public string ToName { get; set; } = default!;
    public string OfficeName { get; set; } = default!;
    public string OfficeNit { get; set; } = default!;
    public string OfficeAddress { get; set; } = default!;
    public string OfficeBrandUrl { get; set; } = default!;
    public string DoctorName { get; set; } = default!;
    public string SpecialityName { get; set; } = default!;
    public string DateAppointment { get; set; } = default!;
    public string StartHour { get; set; } = default!;
    public string EndHour { get; set; } = default!;
    public string? VideoCallLink { get; set; }

    // Pago de la cita (opcional): si viene con enlace, el correo de confirmación
    // muestra el bloque de pago con el monto y el botón "Pagar mi cita".
    public string? CheckoutUrl { get; set; }
    public string? AmountUsd { get; set; }
    public string? AmountCop { get; set; }
}
