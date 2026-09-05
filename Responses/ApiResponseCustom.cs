namespace Backend.Responses;

public class ApiResponseCustom<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = null!;
    public string Path { get; set; } = null!;
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
    public T? Items { get; set; }
}