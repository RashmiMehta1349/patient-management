namespace PatientManagement.Application.Auth.Services;

/// <summary>Generates cryptographically random reset tokens and hashes them for storage.</summary>
public interface IResetTokenGenerator
{
    /// <summary>Returns a URL-safe random raw token (the value sent to the user).</summary>
    string GenerateRawToken();

    /// <summary>Deterministically hashes a raw token (e.g., SHA-256) for storage/lookup.</summary>
    string HashToken(string rawToken);
}
