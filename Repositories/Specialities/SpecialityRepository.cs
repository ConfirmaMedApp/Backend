using Backend.Entities.Specialities;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.Specialities;

public class SpecialityRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, ISpecialityRepository
{
    public Task<Speciality?> CreateAsync(Speciality speciality)
    {
        const string command = "SELECT * FROM create_speciality(@Name, @Code, @Status, @Description);";
        return ExecuteSafeAsync(async conn =>
        {
            var createdSpeciality = await conn.ExecuteScalarAsync<int>(command, new
            {
                Name = speciality.Name.ToLower(),
                Code = speciality.Code.ToLower(),
                speciality.Status,
                Description = speciality.Description?.ToLower()
            });

            return await GetByIdAsync(createdSpeciality);
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<Speciality>> GetAllAsync(int? limit, int? offset, bool? status, string search = "")
    {
        const string query = "SELECT * FROM get_all_specialities(@Limit, @Offset, @Search, @Status);";
        return ExecuteSafeAsync(async conn => await conn.QueryAsync<Speciality>(query, new
        {
            Limit = limit,
            Offset = offset,
            Search = search,
            Status = status
        }), dbConnectionFactory);
    }

    public Task<Speciality?> GetByIdAsync(int id)
    {
        const string query = "SELECT * FROM get_speciality_by_id(@Id);";
        return ExecuteSafeAsync(async conn =>
        {
            var speciality = await conn.QuerySingleOrDefaultAsync<Speciality>(query, new { Id = id });
            return speciality is null ? null : speciality;

        }, dbConnectionFactory);
    }

    public Task<bool> IsAlreadyUsedCode(string code, int? excludedId = null)
    {
        const string query = "SELECT * FROM code_already_used(@Code, @Id);";
        return ExecuteSafeAsync(async conn =>
        {
            var isUsed = await conn.ExecuteScalarAsync<bool>(query, new
            {
                Code = code,
                Id = excludedId
            });
            return isUsed;
        }, dbConnectionFactory);
    }

    public Task<Speciality?> UpdateAsync(Speciality speciality)
    {
        const string command = "SELECT * FROM update_speciality(@Id, @Name, @Code, @Status, @Description);";
        return ExecuteSafeAsync(async conn =>
        {
            var updatedSpecialityId = await conn.ExecuteScalarAsync<int>(command, new
            {
                speciality.Id,
                Name = speciality.Name.ToLower(),
                Code = speciality.Code.ToLower(),
                speciality.Status,
                Description = speciality.Description?.ToLower()
            });
            return await GetByIdAsync(updatedSpecialityId);
        }, dbConnectionFactory);
    }
}
