using Backend.DTOs.MailerSend;
using Backend.Exceptions.NotFound;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Backend.Services.MailerSend;

public class MailerSenderService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IMailerSenderService
{
    private readonly string _apiKey =
        configuration["MailerSend:ApiKey"]
            ?? Environment.GetEnvironmentVariable("Mailer__Send__ApiKey")
            ?? throw new NotFoundException("Key not found");

    private readonly string _fromEmail =
        configuration["MailerSend:FromEmail"]
            ?? Environment.GetEnvironmentVariable("Mailer__Send__From")
            ?? throw new NotFoundException("FromEmail not found");

    private readonly string _fromName =
        configuration["MailerSend:FromName"]
            ?? Environment.GetEnvironmentVariable("Mailer__Send__From__Name")
            ?? throw new NotFoundException("FromName not found");

    public async Task<bool> SendEmailAssignationAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        var client = httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new MailerSendEmailRequestDto(
            from: new MailerSendFrom(_fromEmail, _fromName),
            to: [new MailerSendTo(toEmail, toName)],
            subject,
            html: htmlContent
        );

        var jsonSend = JsonSerializer.Serialize(payload);

        using var content = new StringContent(jsonSend, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(
                "https://api.mailersend.com/v1/email",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(errorContent);
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> SendEmailReminderAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        var client = httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new MailerSendEmailRequestDto(
            from: new MailerSendFrom(_fromEmail, _fromName),
            to: [new MailerSendTo(toEmail, toName)],
            subject,
            html: htmlContent
        );

        var jsonSend = JsonSerializer.Serialize(payload);

        using var content = new StringContent(jsonSend, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(
                "https://api.mailersend.com/v1/email",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(errorContent);
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
