using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auth.Dtos;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Auth.Commands;

/// <summary>
/// Always returns a generic success response regardless of whether the email matches,
/// to avoid user enumeration. If it matches, issues a single-use, time-limited reset token.
/// </summary>
public class ForgotPasswordCommandHandler
{
    public const string GenericResponseMessage = "If that email is registered, a reset link has been sent.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IResetTokenGenerator _tokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly AuthOptions _options;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository resetTokenRepository,
        IResetTokenGenerator tokenGenerator,
        IEmailSender emailSender,
        IDateTimeProvider dateTimeProvider,
        IOptions<AuthOptions> options,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _resetTokenRepository = resetTokenRepository;
        _tokenGenerator = tokenGenerator;
        _emailSender = emailSender;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> HandleAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (user is not null)
            {
                var rawToken = _tokenGenerator.GenerateRawToken();
                var tokenHash = _tokenGenerator.HashToken(rawToken);

                var resetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    TokenHash = tokenHash,
                    ExpiresAt = _dateTimeProvider.UtcNow.AddMinutes(_options.ResetTokenLifetimeMinutes),
                    CreatedAt = _dateTimeProvider.UtcNow
                };

                await _resetTokenRepository.AddAsync(resetToken, cancellationToken);

                var resetLink = $"{_options.ClientResetPasswordUrl}?token={Uri.EscapeDataString(rawToken)}";
                await _emailSender.SendPasswordResetEmailAsync(user.Email, resetLink, cancellationToken);
            }
            else
            {
                // Deliberately do equivalent work whether or not the email matches, to reduce
                // timing-based enumeration (minor given single-user scope, but low-cost to include).
                _tokenGenerator.HashToken(_tokenGenerator.GenerateRawToken());
                _logger.LogInformation("Forgot-password requested for unknown email.");
            }
        }

        return GenericResponseMessage;
    }
}
