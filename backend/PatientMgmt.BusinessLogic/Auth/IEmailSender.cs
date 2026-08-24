namespace PatientMgmt.BusinessLogic.Auth
{
    /// <summary>
    /// Pluggable email dispatch interface (A5 assumption: email-based reset link).
    /// Swappable so an admin-issued-code fallback could be added later without redesign.
    /// </summary>
    public interface IEmailSender
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default);
    }
}
