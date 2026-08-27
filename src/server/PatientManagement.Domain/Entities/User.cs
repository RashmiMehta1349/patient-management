using System;

namespace PatientManagement.Domain.Entities;

/// <summary>
/// The single pre-provisioned physician account (Phase 1: exactly one row).
/// </summary>
public class User
{
    public long Id { get; set; }

    /// <summary>Login identifier.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Output of PasswordHasher&lt;User&gt; — never plaintext.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Embedded as a JWT claim and re-checked on every request so a credential change
    /// invalidates previously issued tokens.
    /// </summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
