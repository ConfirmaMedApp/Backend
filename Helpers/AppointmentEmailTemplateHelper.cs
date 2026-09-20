using Backend.DTOs.MailerSend;
using System.Text.RegularExpressions;
using Backend.Exceptions.NotFound;

namespace Backend.Helpers;

public static class AppointmentEmailTemplateHelper
{
    private static readonly Regex VideoCallBlockRegex =
        new(@"\{\{#VideoCallBlock\}\}(.*?)\{\{/VideoCallBlock\}\}", RegexOptions.Singleline | RegexOptions.Compiled);

    public static string BuildConfirmationEmail(AppointmentConfirmationEmailDto dto)
    {
        var body = LoadTemplate();

        body = ApplyVideoCallBlock(body, dto.VideoCallLink);

        body = body
            .Replace("{{OfficeBrandUrl}}", dto.OfficeBrandUrl)
            .Replace("{{OfficeName}}", dto.OfficeName)
            .Replace("{{ToName}}", dto.ToName)
            .Replace("{{DoctorName}}", dto.DoctorName)
            .Replace("{{SpecialityName}}", dto.SpecialityName)
            .Replace("{{DateAppointment}}", dto.DateAppointment)
            .Replace("{{StartHour}}", dto.StartHour)
            .Replace("{{EndHour}}", dto.EndHour)
            .Replace("{{OfficeNit}}", dto.OfficeNit)
            .Replace("{{OfficeAddress}}", dto.OfficeAddress);

        return body;
    }

    public static string BuildReminderEmail(AppointmentReminderEmailDto dto)
    {
        var body = LoadTemplate();

        body = ApplyVideoCallBlock(body, dto.VideoCallLink);

        body = body
            .Replace("{{OfficeBrandUrl}}", dto.OfficeBrandUrl)
            .Replace("{{OfficeName}}", dto.OfficeName)
            .Replace("{{ToName}}", dto.ToName)
            .Replace("{{DoctorName}}", dto.DoctorName)
            .Replace("{{SpecialityName}}", dto.SpecialityName)
            .Replace("{{DateAppointment}}", dto.DateAppointment)
            .Replace("{{StartHour}}", dto.StartHour)
            .Replace("{{EndHour}}", dto.EndHour)
            .Replace("{{OfficeNit}}", dto.OfficeNit)
            .Replace("{{OfficeAddress}}", dto.OfficeAddress);

        return body;
    }

    private static string LoadTemplate()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "confirmation-email.html");

        if (!File.Exists(templatePath))
        {
            throw new NotFoundException("No se encontró el template del correo electrónico");
        }

        return File.ReadAllText(templatePath);
    }

    private static string ApplyVideoCallBlock(string body, string? videoCallLink)
    {
        if (string.IsNullOrWhiteSpace(videoCallLink))
        {
            return VideoCallBlockRegex.Replace(body, string.Empty);
        }

        return VideoCallBlockRegex.Replace(
            body,
            match => match.Groups[1].Value.Replace("{{VideoCallLink}}", videoCallLink));
    }
}
