using Backend.DTOs.Emails;
using System.Text.RegularExpressions;
using Backend.Exceptions.NotFound;

namespace Backend.Helpers;

public static class AppointmentEmailTemplateHelper
{
    private static readonly Regex VideoCallBlockRegex =
        new(@"\{\{#VideoCallBlock\}\}(.*?)\{\{/VideoCallBlock\}\}", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex PaymentBlockRegex =
        new(@"\{\{#PaymentBlock\}\}(.*?)\{\{/PaymentBlock\}\}", RegexOptions.Singleline | RegexOptions.Compiled);

    public static string BuildConfirmationEmail(AppointmentConfirmationEmailDto dto)
    {
        var body = LoadTemplate("confirmation-email.html");

        body = ApplyVideoCallBlock(body, dto.VideoCallLink);
        body = ApplyPaymentBlock(body, dto);

        body = body
            .Replace("{{OfficeName}}", dto.OfficeName)
            .Replace("{{ToName}}", dto.ToName)
            .Replace("{{DoctorName}}", dto.DoctorName)
            .Replace("{{SpecialityName}}", dto.SpecialityName)
            .Replace("{{DateAppointment}}", dto.DateAppointment)
            .Replace("{{StartHour}}", dto.StartHour)
            .Replace("{{EndHour}}", dto.EndHour)
            .Replace("{{OfficeAddress}}", dto.OfficeAddress);

        return body;
    }

    public static string BuildCancellationEmail(AppointmentCancellationEmailDto dto)
    {
        var body = LoadTemplate("cancellation-email.html");

        body = body
            .Replace("{{OfficeName}}", dto.OfficeName)
            .Replace("{{ToName}}", dto.ToName)
            .Replace("{{DoctorName}}", dto.DoctorName)
            .Replace("{{SpecialityName}}", dto.SpecialityName)
            .Replace("{{DateAppointment}}", dto.DateAppointment)
            .Replace("{{StartHour}}", dto.StartHour)
            .Replace("{{EndHour}}", dto.EndHour)
            .Replace("{{OfficeAddress}}", dto.OfficeAddress);

        return body;
    }

    public static string BuildReminderEmail(AppointmentReminderEmailDto dto)
    {
        var body = LoadTemplate("reminder-email.html");

        body = ApplyVideoCallBlock(body, dto.VideoCallLink);

        body = body
            .Replace("{{OfficeName}}", dto.OfficeName)
            .Replace("{{ToName}}", dto.ToName)
            .Replace("{{DoctorName}}", dto.DoctorName)
            .Replace("{{SpecialityName}}", dto.SpecialityName)
            .Replace("{{DateAppointment}}", dto.DateAppointment)
            .Replace("{{StartHour}}", dto.StartHour)
            .Replace("{{EndHour}}", dto.EndHour)
            .Replace("{{OfficeAddress}}", dto.OfficeAddress);

        return body;
    }

    private static string LoadTemplate(string fileName)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", fileName);

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

    // El bloque de pago solo se muestra si la cita trae un enlace de pago (checkoutUrl).
    // Si no hay enlace (cita sin precio o Wompi no disponible), se elimina el bloque.
    private static string ApplyPaymentBlock(string body, AppointmentConfirmationEmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CheckoutUrl))
        {
            return PaymentBlockRegex.Replace(body, string.Empty);
        }

        return PaymentBlockRegex.Replace(
            body,
            match => match.Groups[1].Value
                .Replace("{{CheckoutUrl}}", dto.CheckoutUrl)
                .Replace("{{AmountUsd}}", dto.AmountUsd ?? string.Empty)
                .Replace("{{AmountCop}}", dto.AmountCop ?? string.Empty));
    }
}
