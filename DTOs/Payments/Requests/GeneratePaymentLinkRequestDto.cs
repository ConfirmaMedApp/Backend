namespace Backend.DTOs.Payments.Requests;

public sealed class GeneratePaymentLinkRequestDto
{
    // Si es true, además de devolver la URL se envía por correo al paciente (Bird).
    public bool SendEmail { get; set; } = false;
}
