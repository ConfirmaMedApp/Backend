using System.Data;

namespace Backend.Persistence;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}