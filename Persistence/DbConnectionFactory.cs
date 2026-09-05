using System.Data;
using Npgsql;

namespace Backend.Persistence;

public class DbConnectionFactory : IDbConnectionFactory, IDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public DbConnectionFactory(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        this._dataSource = builder.Build();
    }
    
    public IDbConnection CreateConnection()
    {
        return this._dataSource.OpenConnection();
    }

    public void Dispose()
    {
        this._dataSource.Dispose();
    }
}