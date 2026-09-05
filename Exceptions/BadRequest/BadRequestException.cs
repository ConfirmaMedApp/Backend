namespace Backend.Exceptions.BadRequest;

public class BadRequestException : Exception
{
    public BadRequestException() {}
    public BadRequestException(string message): base(message) {}
}