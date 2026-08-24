using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PatientManagement.Application.Auth.Services;

namespace PatientManagement.Infrastructure.Services;

/// <summary>
/// Phase 1 stub implementation of <see cref="IEmailSender"/>: writes the reset link to
/// console/logs instead of sending a real email. No SMTP/email provider is configured yet
/// (deferred per plan Assumption A3); swap in a real implementation (e.g., SmtpEmailSender)
/// later without touching auth business logic.
/// </summary>
public class DevConsoleEmailSender : IEmailSender
{
    private readonly ILogger<DevConsoleEmailSender> _logger;

    public DevConsoleEmailSender(ILogger<DevConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL STUB] Password reset requested for {Email}. Reset link: {ResetLink}",
            toEmail, resetLink);
        return Task.CompletedTask;
    }
}
