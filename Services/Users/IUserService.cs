using Backend.DTOs.Users.Requests;
using Backend.DTOs.Users.Responses;

namespace Backend.Services.Users;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "");
    Task<UserResponseDto?> GetByIdAsync(int id);
    Task<UserResponseDto?> CreateAsync(UserRequestCreateDto dto);
    Task<UserResponseDto?> UpdateAsync(UserRequestUpdateDto dto);
}
