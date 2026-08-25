using System.Threading;
using System.Threading.Tasks;

namespace PatientManagement.Application.Auth.Services;

/// <summary>
/// Abstraction over email delivery so a real provider (SMTP/SendGrid/etc.) can be swapped in later
/// without touching auth business logic. The Phase 1 implementation is a console/log-only stub.
/// </summary>
public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
}
