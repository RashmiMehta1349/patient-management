namespace PatientManagement.Application.Auth.Services;

/// <summary>Wraps password hashing/verification so Application code doesn't depend on a concrete hasher.</summary>
public interface IPasswordHasherService
{
    string HashPassword(string password);

    /// <summary>Returns true if <paramref name="providedPassword"/> matches <paramref name="hashedPassword"/>.</summary>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
