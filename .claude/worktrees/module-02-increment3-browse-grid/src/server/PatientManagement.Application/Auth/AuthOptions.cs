namespace PatientManagement.Application.Auth;

/// <summary>
/// Externalized auth configuration (Assumptions A2/A4) — bound from appsettings/environment
/// so lifetimes can change without code changes.
/// </summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>JWT signing key (symmetric). Read from configuration/environment — never hardcoded.</summary>
    public string JwtSigningKey { get; set; } = string.Empty;

    public string JwtIssuer { get; set; } = "PatientManagement";

    public string JwtAudience { get; set; } = "PatientManagement.Client";

    /// <summary>Access token lifetime in minutes (also drives the client inactivity timeout — A2/A4).</summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 20;

    /// <summary>Password reset token lifetime in minutes (A4).</summary>
    public int ResetTokenLifetimeMinutes { get; set; } = 30;

    /// <summary>Base URL used to build the reset-password link sent to the user.</summary>
    public string ClientResetPasswordUrl { get; set; } = "http://localhost:4200/reset-password";
}
