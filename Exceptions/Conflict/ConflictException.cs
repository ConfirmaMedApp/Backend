namespace Backend.Exceptions.Conflict;

public class ConflictException : Exception
{
    public ConflictException() { }
    public ConflictException(string message): base(message) {}
}