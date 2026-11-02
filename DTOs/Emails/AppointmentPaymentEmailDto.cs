namespace Backend.DTOs.Emails;

public class AppointmentPaymentEmailDto
{
    public string ToName { get; set; } = default!;
    public string OfficeName { get; set; } = default!;
    public string OfficeAddress { get; set; } = default!;
    public string DoctorName { get; set; } = default!;
    public string SpecialityName { get; set; } = default!;
    public string DateAppointment { get; set; } = default!;
    public string StartHour { get; set; } = default!;
    public string EndHour { get; set; } = default!;
    public string AmountUsd { get; set; } = default!;
    public string AmountCop { get; set; } = default!;
    public string CheckoutUrl { get; set; } = default!;
    public string? ExpiresAt { get; set; }
}
