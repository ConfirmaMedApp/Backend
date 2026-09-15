using Backend.DTOs.Users.Responses;
using Backend.Entities.Users;
using Backend.Persistence;
using BCrypt.Net;
using Dapper;

namespace Backend.Repositories.Users;

public class UserRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IUserRepository
{
    public Task<UserFlatDto?> CreateAsync(User user)
    {
        const string command = "SELECT * FROM create_user(@Name, @Lastname, @Email, @Username, @Password, @OfficeId, @DoctorId, @Status, @Role);";
        return ExecuteSafeAsync(async conn =>
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password, 10);

            var newUser = await conn.ExecuteScalarAsync<int>(command, new
            {
                Name = user.Name.ToLower(),
                Lastname = user.Lastname.ToLower(),
                Email = user.Email.ToLower(),
                user.Username,
                user.Password,
                user.OfficeId,
                user.DoctorId,
                user.Status,
                user.Role
            });

            return await GetByIdAsync(newUser);
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<UserFlatDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "")
    {
        const string query = "SELECT * FROM get_all_users(@Limit, @Offset, @Search, @Status);";
        return ExecuteSafeAsync(async conn => await conn.QueryAsync<UserFlatDto>(query, new { Limit = limit, Offset = offset, Search = search, Status = status }), dbConnectionFactory);
    }

    public Task<UserFlatDto?> GetByIdAsync(int? id)
    {
        const string query = "SELECT * FROM get_user_by_id(@Id);";
        return ExecuteSafeAsync(async conn =>
        {
            var user = await conn.QueryFirstOrDefaultAsync<UserFlatDto>(query, new { Id = id });
            return user;
        }, dbConnectionFactory);
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        const string query = "SELECT * FROM get_user_by_username(@Username);";
        return ExecuteSafeAsync(async conn =>
        {
            var user = await conn.QueryFirstOrDefaultAsync<User>(query, new { Username = username });
            return user;
        }, dbConnectionFactory);
    }

    public Task<bool> IsUsernameAlreadyUsed(string username, int? userId = null)
    {
        const string query = "SELECT * FROM username_already_used(@Username, @Id);";
        return ExecuteSafeAsync(async conn =>
        {
            var isUsed = await conn.QueryFirstOrDefaultAsync<bool>(query, new { Username = username, Id = userId });
            return isUsed;
        }, dbConnectionFactory);
    }

    public Task<UserFlatDto?> UpdateAsync(User user)
    {
        const string command = "SELECT * FROM update_user(@Id, @Name, @Lastname, @Email, @Username, @OfficeId, @DoctorId, @Status, @Role);";
        return ExecuteSafeAsync(async conn =>
        {    
            var updatedUser = await conn.ExecuteScalarAsync<int>(command, new
            {
                user.Id,
                Name = user.Name.ToLower(),
                Lastname = user.Lastname.ToLower(),
                Email = user.Email.ToLower(),
                user.Username,
                user.Password,
                user.OfficeId,
                user.DoctorId,
                user.Status,
                user.Role
            });
            return await GetByIdAsync(updatedUser);
        }, dbConnectionFactory);
    }
}
