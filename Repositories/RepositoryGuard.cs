using System.Data;
using Backend.Exceptions.DatabaseOperation;
using Backend.Persistence;

namespace Backend.Repositories;

public abstract class RepositoryGuard
{
    protected async Task<T> ExecuteSafeAsync<T>(
        Func<IDbConnection, Task<T>> action,
        IDbConnectionFactory factory
    )
    {
        try
        {
            using var connection = factory.CreateConnection();
            return await action(connection);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException(
                ex.InnerException?.Message ?? ex.Message,
                ex
            );
        }
    }
}