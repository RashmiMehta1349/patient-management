using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PatientManagement.Application.Auth.Dtos;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Common;

namespace PatientManagement.Application.Auth.Commands;

/// <summary>
/// Consumes a password reset token: validates hash match, expiry, and single-use, then
/// updates the password hash and regenerates SecurityStamp to invalidate all prior sessions.
/// </summary>
public class ResetPasswordCommandHandler
{
    public const string InvalidOrExpiredTokenMessage = "This reset link is invalid or has expired.";

    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IResetTokenGenerator _tokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository resetTokenRepository,
        IUserRepository userRepository,
        IPasswordHasherService passwordHasher,
        IResetTokenGenerator tokenGenerator,
        IDateTimeProvider dateTimeProvider,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _resetTokenRepository = resetTokenRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result<bool>.Failure(InvalidOrExpiredTokenMessage);
        }

        var tokenHash = _tokenGenerator.HashToken(request.Token);
        var resetToken = await _resetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        if (resetToken is null || resetToken.IsConsumed || resetToken.IsExpired(now))
        {
            _logger.LogWarning("Reset-password attempt with invalid, expired, or consumed token.");
            return Result<bool>.Failure(InvalidOrExpiredTokenMessage);
        }

        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.Failure(InvalidOrExpiredTokenMessage);
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = now;
        await _userRepository.UpdateAsync(user, cancellationToken);

        resetToken.ConsumedAt = now;
        await _resetTokenRepository.UpdateAsync(resetToken, cancellationToken);

        return Result<bool>.Success(true);
    }
}
