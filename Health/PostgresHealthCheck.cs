using Backend.Persistence;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Backend.Health;

public class PostgresHealthCheck(IDbConnectionFactory dbConnectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = dbConnectionFactory.CreateConnection();
            await connection.ExecuteScalarAsync<int>("SELECT 1", commandTimeout: 2);

            return HealthCheckResult.Healthy("PostgreSQL is healthy.");
        }
        catch (Exception ex) 
        {
            return HealthCheckResult.Unhealthy(
                "Postgres unavailable",
                ex);
        }
    }
}
