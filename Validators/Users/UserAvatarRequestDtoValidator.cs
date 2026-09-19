using Backend.DTOs.Users.Requests;
using Backend.Entities.Users;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Backend.Validators.Users;

public class UserAvatarRequestDtoValidator : AbstractValidator<UserAvatarRequestDto>
{
    private static readonly string[] AllowedMimeTypes =
        ["image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"];

    private const long MaxFileBytes = 2 * 1024 * 1024; // 2 MB

    public UserAvatarRequestDtoValidator(IOptions<UserAvatarPresetsSettings> presetsOptions)
    {
        var presets = presetsOptions.Value.Items;

        RuleFor(x => x)
            .Must(x => (x.File is not null) ^ !string.IsNullOrWhiteSpace(x.PresetKey))
            .WithMessage("Debes enviar un preset o un archivo, no ambos");

        When(x => !string.IsNullOrWhiteSpace(x.PresetKey), () =>
        {
            RuleFor(x => x.PresetKey!)
                .Must(key => presets.ContainsKey(key) && !string.IsNullOrWhiteSpace(presets[key]))
                .WithMessage("El preset seleccionado no es válido");
        });

        When(x => x.File is not null, () =>
        {
            RuleFor(x => x.File!)
                .Must(f => f.Length > 0).WithMessage("El archivo está vacío")
                .Must(f => f.Length <= MaxFileBytes).WithMessage("El archivo no debe superar 2 MB")
                .Must(f => AllowedMimeTypes.Contains(f.ContentType.ToLower()))
                .WithMessage("El archivo debe ser una imagen (JPEG, PNG, GIF o WebP)");
        });
    }
}
