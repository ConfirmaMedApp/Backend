using System.Security.Cryptography;
using System.Text;

namespace Backend.Helpers;

public static class RefreshTokenHelper
{
    // Token opaco de alta entropia (64 bytes) en formato url-safe.
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    // SHA-256 en hex. Suficiente para tokens aleatorios de alta entropia
    // (no es una contrasena, no requiere BCrypt) y permite WHERE token_hash = ...
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
