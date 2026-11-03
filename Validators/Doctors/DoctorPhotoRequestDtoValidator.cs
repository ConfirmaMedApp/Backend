using Backend.DTOs.Doctors.Requests;
using FluentValidation;

namespace Backend.Validators.Doctors;

public class DoctorPhotoRequestDtoValidator : AbstractValidator<DoctorPhotoRequestDto>
{
    private static readonly string[] AllowedMimeTypes =
        ["image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"];

    private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB

    public DoctorPhotoRequestDtoValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("Debes enviar un archivo de imagen");

        When(x => x.File is not null, () =>
        {
            RuleFor(x => x.File!)
                .Must(f => f.Length > 0).WithMessage("El archivo está vacío")
                .Must(f => f.Length <= MaxFileBytes).WithMessage("El archivo no debe superar 5 MB")
                .Must(f => AllowedMimeTypes.Contains(f.ContentType.ToLower()))
                .WithMessage("El archivo debe ser una imagen (JPEG, PNG, GIF o WebP)");
        });
    }
}
