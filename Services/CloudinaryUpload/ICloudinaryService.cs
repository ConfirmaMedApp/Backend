namespace Backend.Services.CloudinaryUpload;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file, string? folder = null);
    Task<string> UploadVideoAsync(IFormFile file, string? folder = null);
    Task<bool> DeleteFileAsync(string publicId);
}
