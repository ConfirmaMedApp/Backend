namespace Backend.Exceptions.Unauthorized;

public class UnauthorizedException : Exception
{
    public UnauthorizedException() { }
    public UnauthorizedException(string message): base(message) {}
}