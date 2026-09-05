using Backend.DTOs.Offices.Responses;
using Backend.DTOs.Patients.Responses;

namespace Backend.Services.MailerSend;

public interface IMailerSenderService
{
    Task<bool> SendEmailAssignationAsync(string toEmail, string toName, string subject, string htmlContent);
    public Task<bool> SendEmailReminderAsync(string toEmail, string toName, string subject, string htmlContent);
}
