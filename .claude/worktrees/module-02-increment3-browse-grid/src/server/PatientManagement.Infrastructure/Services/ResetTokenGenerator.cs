using System.Security.Cryptography;
using PatientManagement.Application.Auth.Services;

namespace PatientManagement.Infrastructure.Services;

public class ResetTokenGenerator : IResetTokenGenerator
{
    public string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    public string HashToken(string rawToken)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        return System.Convert.ToHexString(hashBytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        System.Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
