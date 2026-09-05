namespace Backend.Exceptions.Forbidden;

public class ForbiddenException : Exception
{
    public ForbiddenException() { }
    public ForbiddenException(string message): base(message) {}
}