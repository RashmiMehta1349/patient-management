using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Auth.Services;

namespace PatientManagement.Tests.Integration.Auth;

/// <summary>Captures the last reset link sent, so integration tests can extract the raw token
/// without needing a real email transport — mirrors the role the dev console stub plays at
/// runtime, but makes the value assertable in-process.</summary>
public class TestEmailSender : IEmailSender
{
    public string? LastResetLink { get; private set; }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        LastResetLink = resetLink;
        return Task.CompletedTask;
    }
}
