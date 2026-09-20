namespace Backend.Entities.Daily;

public class DailySettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.daily.co/v1/";
    public string SubDomain { get; set; } = string.Empty;
    public int MaxCallDurationMinutes { get; set; } = 45;
    public int MonthlyRoomsLimit { get; set; } = 30;
    public int ExpirationGraceMinutes { get; set; } = 5;
}
