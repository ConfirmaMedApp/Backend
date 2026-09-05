using Backend.Entities.Durations;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.Durations;

public class DurationRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IDurationRepository
{
    public Task<IEnumerable<Duration>> GetAllAsync()
    {
        const string query = "SELECT * FROM get_all_durations();";

        return ExecuteSafeAsync(async connection =>
        {
            var durations = await connection.QueryAsync<Duration>(query);
            return durations;
        }, dbConnectionFactory);
    }
}
