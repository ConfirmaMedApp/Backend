using Backend.DTOs.Bird;
using Backend.Exceptions.NotFound;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Backend.Services.Bird;

public class BirdEmailService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IBirdEmailService
{
    private readonly string _apiKey =
        configuration["Bird:ApiKey"]
            ?? Environment.GetEnvironmentVariable("Bird__ApiKey")
            ?? throw new NotFoundException("Bird ApiKey not found");

    private readonly string _baseUrl =
        (configuration["Bird:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("Bird__BaseUrl")
            ?? "https://us1.platform.bird.com").TrimEnd('/');

    private readonly string _fromEmail =
        configuration["Bird:FromEmail"]
            ?? Environment.GetEnvironmentVariable("Bird__FromEmail")
            ?? throw new NotFoundException("Bird FromEmail not found");

    private readonly string _fromName =
        configuration["Bird:FromName"]
            ?? Environment.GetEnvironmentVariable("Bird__FromName")
            ?? throw new NotFoundException("Bird FromName not found");

    public Task<bool> SendEmailAssignationAsync(string toEmail, string toName, string subject, string htmlContent)
        => SendAsync(toEmail, toName, subject, htmlContent);

    public Task<bool> SendEmailReminderAsync(string toEmail, string toName, string subject, string htmlContent)
        => SendAsync(toEmail, toName, subject, htmlContent);

    private async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        var client = httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new BirdEmailRequestDto(
            from: new BirdEmailAddress(_fromEmail, _fromName),
            to: [new BirdEmailAddress(toEmail, toName)],
            subject,
            html: htmlContent,
            category: "transactional"
        );

        var jsonSend = JsonSerializer.Serialize(payload);

        using var content = new StringContent(jsonSend, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(
                $"{_baseUrl}/v1/email/messages",
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
