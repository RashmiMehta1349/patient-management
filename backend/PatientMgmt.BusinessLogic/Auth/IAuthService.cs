namespace PatientMgmt.BusinessLogic.Auth
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default);
        Task LogoutAsync(Guid sessionId, CancellationToken ct = default);

        /// <summary>
        /// Validates a session by ID: checks IsValid flag, hard expiry, and idle timeout (A4).
        /// On success, refreshes LastActivityAt (sliding idle window).
        /// </summary>
        Task<SessionValidationResult> ValidateSessionAsync(Guid sessionId, CancellationToken ct = default);
    }
}
