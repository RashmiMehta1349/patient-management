namespace PatientMgmt.BusinessLogic.Auth
{
    public class LoginResult
    {
        public bool Success { get; init; }
        public string? AccessToken { get; init; }
        public DateTime? ExpiresAt { get; init; }

        public static LoginResult Fail() => new() { Success = false };

        public static LoginResult Ok(string token, DateTime expiresAt) =>
            new() { Success = true, AccessToken = token, ExpiresAt = expiresAt };
    }

    public class SessionValidationResult
    {
        public bool IsValid { get; init; }
        public Guid? UserId { get; init; }
        public string? Email { get; init; }

        public static SessionValidationResult Invalid() => new() { IsValid = false };

        public static SessionValidationResult Valid(Guid userId, string email) =>
            new() { IsValid = true, UserId = userId, Email = email };
    }
}
