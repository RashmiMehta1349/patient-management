namespace PatientMgmt.BusinessLogic.Auth
{
    public interface IPasswordResetService
    {
        /// <summary>
        /// Always "succeeds" from the caller's perspective (no enumeration signal). Internally,
        /// only generates/sends a token if the email matches the single provisioned account.
        /// </summary>
        Task RequestResetAsync(string email, CancellationToken ct = default);

        /// <summary>
        /// Validates the raw token, updates the password hash, marks the token used, and
        /// invalidates all existing sessions for the user (Business Rule).
        /// </summary>
        Task<ResetCompletionResult> CompleteResetAsync(string rawToken, string newPassword, CancellationToken ct = default);
    }
}
