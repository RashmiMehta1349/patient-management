using System;

namespace PatientMgmt.Domain.Entities
{
    /// <summary>
    /// Server-tracked session record referenced by the JWT's "sid" claim.
    /// Bridges stateless JWTs with revocability (idle timeout + password-reset invalidation).
    /// </summary>
    public class Session
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public bool IsValid { get; set; } = true;
        public DateTime ExpiresAt { get; set; }
    }
}
