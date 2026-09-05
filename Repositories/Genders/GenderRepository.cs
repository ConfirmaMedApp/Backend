using Backend.Entities.Genders;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.Genders;

public class GenderRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IGenderRepository
{
    public Task<IEnumerable<Gender>> GetAllAsync()
    {
        const string query = "SELECT * FROM get_all_genders();";
        return ExecuteSafeAsync(async conn => await conn.QueryAsync<Gender>(query),
            dbConnectionFactory);
    }
}