using Backend.Entities.Offices;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.Offices;

public class OfficeRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IOfficeRepository
{
    public Task<IEnumerable<Office>> GetAllAsync()
    {
        const string query = "SELECT * FROM get_all_offices();";
        return ExecuteSafeAsync(async conn => await conn.QueryAsync<Office>(query),
            dbConnectionFactory);
    }

    public Task<Office?> GetByIdAsync(int id)
    {
        const string query = "SELECT * FROM get_office_by_id(@Id);";
        return ExecuteSafeAsync(async conn =>
        {
            var office = await conn.QueryFirstOrDefaultAsync<Office>(query, new { Id = id });
            return office is null ? null : office;
        }, dbConnectionFactory);
    }

    public Task<Office?> GetByUserIdAsync(int userId)
    {
        const string query = "SELECT * FROM get_office_by_user_id(@UserId);";
        return ExecuteSafeAsync(async conn =>
        {
            var office = await conn.QueryFirstOrDefaultAsync<Office>(query, new { UserId = userId });
            return office is null ? null : office;
        }, dbConnectionFactory);
    }

    public Task<Office?> CreateAsync(Office office)
    {
        const string command = "SELECT * FROM create_office(@Name::text, @Nit::varchar, @Brand::text, @Address::text, @Description::text, @Status::boolean);";
        return ExecuteSafeAsync(async conn =>
        {
            var newOffice = await conn.ExecuteScalarAsync<int>(command, new
            {
                Name = office.Name.ToLower(),
                Nit = office.Nit.ToLower(),
                office.Brand,
                Address = office.Address.ToLower(),
                Description = office.Description?.ToLower(),
                office.Status
            });
            
            return await GetByIdAsync(newOffice);
        }, dbConnectionFactory);
    }

    public Task<Office?> UpdateAsync(Office office)
    {
        const string command = "SELECT * FROM update_office(@Id, @Name::text, @Nit::varchar, @Brand::text, @Address::text, @Description::text, @Status::boolean);";
        return ExecuteSafeAsync(async conn =>
        {
            var updateOfficeId = await conn.ExecuteScalarAsync<int>(command, new
            {
                office.Id,
                office.Name,
                office.Nit,
                office.Brand,
                office.Address,
                office.Description,
                office.Status
            });
            
            return await GetByIdAsync(office.Id);
        }, dbConnectionFactory);
    }

    public Task<bool> IsNitUniqueAsync(string nit, int? excludingOfficeId = null)
    {
        const string query = "SELECT * FROM nit_already_used(@Nit::varchar, @ExcludingOfficeId);";
        return ExecuteSafeAsync(async conn =>
        {
            var isUnique = await conn.ExecuteScalarAsync<bool>(query, new
            {
                Nit = nit,
                ExcludingOfficeId = excludingOfficeId
            });

            return isUnique;
        }, dbConnectionFactory);
    }
}