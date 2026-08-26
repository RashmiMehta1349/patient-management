using System;

namespace PatientManagement.Domain.Entities;

/// <summary>
/// A single-use, time-limited token that authorizes a password reset.
/// The raw token is never stored — only its SHA-256 hash.
/// </summary>
public class PasswordResetToken
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public User? User { get; set; }

    /// <summary>SHA-256 hash of the raw token issued to the user.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the token is successfully used; null = still valid/unused.</summary>
    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAt;

    public bool IsConsumed => ConsumedAt.HasValue;
}
