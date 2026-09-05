using Backend.Entities.CloudinaryUpload;
using Backend.Exceptions.BadRequest;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Backend.Services.CloudinaryUpload;

public class CloudinaryService(IOptions<CloudinarySettings> config, ILogger<CloudinaryService> logger) : ICloudinaryService
{
    private readonly Cloudinary cloudinary = new(new Account(
        config.Value.CloudName,
        config.Value.ApiKey,
        config.Value.ApiSecret
    ))
    {
        Api = { Secure = true }
    };

    public Task<bool> DeleteFileAsync(string publicId)
    {
        throw new NotImplementedException();
    }

    public async Task<string> UploadImageAsync(IFormFile file, string? folder = null)
    {
        if (file.Length == 0)
            throw new ArgumentException("El archivo está vacío");

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            throw new ArgumentException("El archivo debe ser una imagen (JPEG, PNG, GIF o WebP)");

        try
        {
            using var stream = file.OpenReadStream();
            
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "appointments_annexes" + folder ?? "/",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                logger.LogError("Error al subir imagen a Cloudinary: {Error}", uploadResult.Error.Message);
                throw new BadRequestException($"Error al subir la imagen: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado al subir imagen");
            throw;
        }
    }

    public Task<string> UploadVideoAsync(IFormFile file, string? folder = null)
    {
        throw new NotImplementedException();
    }
}
