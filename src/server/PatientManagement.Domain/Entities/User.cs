using System;
using System.Collections.Generic;

namespace PatientManagement.Domain.Entities;

/// <summary>
/// The single pre-provisioned physician account (Phase 1: exactly one row).
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Login identifier.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Output of PasswordHasher&lt;User&gt; — never plaintext.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Regenerated on password reset; embedded as a JWT claim and re-checked
    /// on every request so previously issued tokens are invalidated.
    /// </summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
