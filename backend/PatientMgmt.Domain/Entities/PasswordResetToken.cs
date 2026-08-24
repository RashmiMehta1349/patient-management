using System;

namespace PatientMgmt.Domain.Entities
{
    /// <summary>
    /// Single-use, time-limited password reset token. Only the hash is persisted;
    /// the raw token exists only in the emailed link and transiently in the request body.
    /// </summary>
    public class PasswordResetToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
