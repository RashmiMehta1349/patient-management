namespace PatientMgmt.Domain.Options
{
    /// <summary>
    /// Configuration-driven auth settings (appsettings.json section "Auth").
    /// Idle timeout (A4) and reset token lifetime (A6) are settings, not hardcoded
    /// constants, per the plan's risk mitigation so they can change without a
    /// redeploy of logic.
    /// </summary>
    public class AuthOptions
    {
        public const string SectionName = "Auth";

        /// <summary>Symmetric signing key for JWTs. Must be provided via secure configuration in production.</summary>
        public string JwtSigningKey { get; set; } = string.Empty;

        public string JwtIssuer { get; set; } = "PatientMgmt";

        public string JwtAudience { get; set; } = "PatientMgmt.Client";

        /// <summary>Hard cap on token/session lifetime, independent of idle timeout.</summary>
        public int SessionHardExpiryMinutes { get; set; } = 720; // 12 hours

        /// <summary>A4 assumption: 15 minutes idle timeout, pending Product Owner confirmation.</summary>
        public int IdleTimeoutMinutes { get; set; } = 15;

        /// <summary>A6 assumption: 30 minute reset token lifetime.</summary>
        public int ResetTokenLifetimeMinutes { get; set; } = 30;

        /// <summary>A7: basic rate limiting window/threshold for public auth endpoints.</summary>
        public int RateLimitMaxAttempts { get; set; } = 5;

        public int RateLimitWindowMinutes { get; set; } = 15;
    }
}
