using System;

namespace PatientMgmt.Domain.Entities
{
    /// <summary>
    /// Exactly one row per Business Rule (Module 1). No in-app account creation UI in Phase 1.
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
