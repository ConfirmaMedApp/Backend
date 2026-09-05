using Backend.DTOs.Users.Responses;
using Backend.Entities.Users;

namespace Backend.Repositories.Users;

public interface IUserRepository
{
    Task<IEnumerable<UserFlatDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "");
    Task<UserFlatDto?> GetByIdAsync(int? id);
    Task<User?> GetByUsernameAsync(string username);
    Task<UserFlatDto?> CreateAsync(User user);
    Task<UserFlatDto?> UpdateAsync(User user);

    // Validations
    Task<bool> IsUsernameAlreadyUsed(string username, int? userId = null);
}
