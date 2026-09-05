namespace Backend.Exceptions.NotFound;

public class NotFoundException : Exception
{
    public NotFoundException() { }
    public NotFoundException(string message): base(message) {}
}