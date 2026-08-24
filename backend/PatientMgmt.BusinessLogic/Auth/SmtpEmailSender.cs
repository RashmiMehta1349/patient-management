using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PatientMgmt.Domain.Options;

namespace PatientMgmt.BusinessLogic.Auth
{
    /// <summary>
    /// SMTP-based implementation of IEmailSender (A5). Configuration-driven so the
    /// deployment environment supplies real credentials; logs (without secrets) if
    /// dispatch fails so a failed password-recovery attempt is never silent.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default)
        {
            try
            {
                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.UseSsl,
                    Credentials = string.IsNullOrEmpty(_options.Username)
                        ? CredentialCache.DefaultNetworkCredentials
                        : new NetworkCredential(_options.Username, _options.Password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
                    Subject = "Password Reset Request",
                    Body = $"A password reset was requested for your Patient Management account.\n\n" +
                           $"Click the link below to set a new password. This link expires in a short time and can only be used once:\n\n" +
                           $"{resetLink}\n\n" +
                           $"If you did not request this, you can safely ignore this email.",
                    IsBodyHtml = false
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message, ct);
            }
            catch (Exception ex)
            {
                // Never log the raw token/link contents beyond what's necessary; do not leak PHI.
                _logger.LogError(ex, "Failed to send password reset email.");
                throw;
            }
        }
    }
}
