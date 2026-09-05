namespace Backend.Exceptions.DatabaseOperation;

public class DatabaseOperationException : Exception
{
    public DatabaseOperationException() { }
    public DatabaseOperationException(string message) : base(message) { }
    public DatabaseOperationException(string message, Exception inner): base(message, inner) {}
}