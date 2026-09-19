using AutoMapper;
using Backend.DTOs.Users.Requests;
using Backend.DTOs.Users.Responses;
using Backend.Entities.Users;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Exceptions.Unauthorized;
using Backend.Repositories.Users;
using Backend.Services.CloudinaryUpload;
using Backend.Services.CurrentUser;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Backend.Services.Users;

public class UserService(
    IUserRepository userRepository,
    IMapper mapper,
    IValidator<UserRequestCreateDto> createValidatorDto,
    IValidator<UserRequestUpdateDto> updateValidatorDto,
    IValidator<UserAvatarRequestDto> avatarValidatorDto,
    ICurrentUserService currentUserService,
    ICloudinaryService cloudinaryService,
    IOptions<UserAvatarPresetsSettings> avatarPresetsOptions
) : IUserService
{
    private readonly Dictionary<string, string> _presets = avatarPresetsOptions.Value.Items;

    public async Task<UserResponseDto?> CreateAsync(UserRequestCreateDto dto)
    {
        var validationResult = await createValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var loggedUserId = currentUserService.UserId;

        var loggedUser = await userRepository.GetByIdAsync(loggedUserId)
                         ?? throw new UnauthorizedException("No has iniciado sesión");

        var officeId = loggedUser.OfficeId;

        var userEntity = mapper.Map<User>(dto);

        userEntity.OfficeId = officeId;
        userEntity.AvatarUrl = ResolvePresetUrl(dto.AvatarPresetKey);

        var createdUser = await userRepository.CreateAsync(userEntity);

        return createdUser is null
            ? throw new BadRequestException("No se ha podido crear el usuario")
            : mapper.Map<UserResponseDto>(createdUser);
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "")
    {
        var users = await userRepository.GetAllAsync(limit, offset, status, search);
        return mapper.Map<IEnumerable<UserResponseDto>>(users);
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await userRepository.GetByIdAsync(id);
        return user is null ? throw new NotFoundException("Usuario no encontrado") : mapper.Map<UserResponseDto>(user);
    }

    public async Task<UserResponseDto?> UpdateAsync(UserRequestUpdateDto dto)
    {
        var validationResult = await updateValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var loggedUserId = currentUserService.UserId;

        var loggedUser = await userRepository.GetByIdAsync(loggedUserId)
                         ?? throw new UnauthorizedException("No has iniciado sesión");

        var officeId = loggedUser.OfficeId;

        var userEntity = mapper.Map<User>(dto);

        userEntity.OfficeId = officeId;

        var updatedUser = await userRepository.UpdateAsync(userEntity);

        return updatedUser is null
            ? throw new NotFoundException("Usuario no encontrado")
            : mapper.Map<UserResponseDto>(updatedUser);
    }

    public async Task<UserResponseDto?> UpdateAvatarAsync(int userId, UserAvatarRequestDto dto)
    {
        var validationResult = await avatarValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var loggedUserId = currentUserService.UserId
                          ?? throw new UnauthorizedException("No has iniciado sesión");

        if (loggedUserId != userId)
            throw new UnauthorizedException("Solo puedes cambiar tu propio avatar");

        _ = await userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("Usuario no encontrado");

        string avatarUrl;
        if (dto.File is not null)
        {
            avatarUrl = await cloudinaryService.UploadImageAsync(dto.File, "users/avatars");
        }
        else
        {
            avatarUrl = ResolvePresetUrl(dto.PresetKey)
                        ?? throw new BadRequestException("Debes indicar un preset o un archivo");
        }

        var updatedUser = await userRepository.UpdateAvatarAsync(userId, avatarUrl)
                          ?? throw new NotFoundException("Usuario no encontrado");

        return mapper.Map<UserResponseDto>(updatedUser);
    }

    public IEnumerable<UserAvatarPresetDto> GetPresets() =>
        _presets
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => new UserAvatarPresetDto { Key = kv.Key, Url = kv.Value });

    private string? ResolvePresetUrl(string? presetKey)
    {
        if (string.IsNullOrWhiteSpace(presetKey)) return null;

        if (!_presets.TryGetValue(presetKey, out var url) || string.IsNullOrWhiteSpace(url))
            throw new BadRequestException($"El avatar '{presetKey}' no existe");

        return url;
    }
}
