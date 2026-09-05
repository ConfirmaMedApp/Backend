using Backend.DTOs.MailerSend;
using System.Reflection;
using Backend.Exceptions.NotFound;

namespace Backend.Helpers;

public static class AppointmentEmailTemplateHelper
{
    public static string BuildConfirmationEmail(AppointmentConfirmationEmailDto dto)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "confirmation-email.html");

        if (!File.Exists(templatePath))
        {
            throw new NotFoundException("No se encontró el template del correo electrónico");
        }
        
        string body = File.ReadAllText(templatePath);

        body = body
            .Replace("{{OfficeBrandUrl}}", dto.OfficeBrandUrl)
            .Replace("{{OfficeName}}", dto.OfficeName)
            .Replace("{{ToName}}", dto.ToName)
            .Replace("{{DateAppointment}}", dto.DateAppointment)
            .Replace("{{StartHour}}", dto.StartHour)
            .Replace("{{EndHour}}", dto.EndHour)
            .Replace("{{OfficeNit}}", dto.OfficeNit)
            .Replace("{{OfficeAddress}}", dto.OfficeAddress);

        return body;
    }

    public static string BuildReminderEmail(AppointmentReminderEmailDto dto)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "confirmation-email.html");

        if (!File.Exists(templatePath))
        {
            throw new NotFoundException("No se encontró el template del correo electrónico");
        }
        
        string body = File.ReadAllText(templatePath);

        body = body
            .Replace("{{OfficeBrandUrl}}", dto.OfficeBrandUrl)
            .Replace("{{OfficeName}}", dto.OfficeName)
            .Replace("{{ToName}}", dto.ToName)
            .Replace("{{DateAppointment}}", dto.DateAppointment)
            .Replace("{{StartHour}}", dto.StartHour)
            .Replace("{{EndHour}}", dto.EndHour)
            .Replace("{{OfficeNit}}", dto.OfficeNit)
            .Replace("{{OfficeAddress}}", dto.OfficeAddress);

        return body;
    }
}
