using Backend.DTOs.Emails;
using Backend.Exceptions.NotFound;

namespace Backend.Helpers;

public static class PaymentEmailTemplateHelper
{
    public static string BuildPaymentEmail(AppointmentPaymentEmailDto dto)
    {
        var body = LoadTemplate("payment-email.html");

        body = body
            .Replace("{{ToName}}", dto.ToName)
            .Replace("{{OfficeName}}", dto.OfficeName)
            .Replace("{{OfficeAddress}}", dto.OfficeAddress)
            .Replace("{{DoctorName}}", dto.DoctorName)
            .Replace("{{SpecialityName}}", dto.SpecialityName)
            .Replace("{{DateAppointment}}", dto.DateAppointment)
            .Replace("{{StartHour}}", dto.StartHour)
            .Replace("{{EndHour}}", dto.EndHour)
            .Replace("{{AmountUsd}}", dto.AmountUsd)
            .Replace("{{AmountCop}}", dto.AmountCop)
            .Replace("{{CheckoutUrl}}", dto.CheckoutUrl)
            .Replace("{{ExpiresAt}}", dto.ExpiresAt ?? string.Empty);

        return body;
    }

    private static string LoadTemplate(string fileName)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", fileName);

        if (!File.Exists(templatePath))
            throw new NotFoundException("No se encontró el template del correo electrónico");

        return File.ReadAllText(templatePath);
    }
}
