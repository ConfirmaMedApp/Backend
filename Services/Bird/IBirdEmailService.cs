namespace Backend.Services.Bird;

public interface IBirdEmailService
{
    Task<bool> SendEmailAssignationAsync(string toEmail, string toName, string subject, string htmlContent);
    Task<bool> SendEmailReminderAsync(string toEmail, string toName, string subject, string htmlContent);
}
