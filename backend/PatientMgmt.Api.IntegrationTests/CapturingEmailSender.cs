using System.Collections.Concurrent;
using PatientMgmt.BusinessLogic.Auth;

namespace PatientMgmt.Api.IntegrationTests
{
    /// <summary>Test double for IEmailSender: captures reset links instead of sending real email.</summary>
    public class CapturingEmailSender : IEmailSender
    {
        public ConcurrentBag<(string ToEmail, string ResetLink)> SentEmails { get; } = new();

        public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default)
        {
            SentEmails.Add((toEmail, resetLink));
            return Task.CompletedTask;
        }
    }
}
