using Microsoft.AspNetCore.Identity;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Infrastructure.Services;

/// <summary>
/// Wraps ASP.NET Core Identity's PasswordHasher&lt;User&gt; (PBKDF2, salted) — avoids
/// inventing custom crypto, satisfies "hashed at rest."
/// </summary>
public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(string password) =>
        _hasher.HashPassword(new User(), password);

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(new User(), hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
