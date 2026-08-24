using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PatientManagement.Application.Auth.Dtos;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Common;

namespace PatientManagement.Application.Auth.Commands;

/// <summary>
/// Validates credentials and issues a JWT on success. Always returns the same generic
/// failure message for wrong-password vs. unknown-email, to avoid revealing which field failed.
/// </summary>
public class LoginCommandHandler
{
    public const string GenericFailureMessage = "Invalid email or password.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<LoginResponseDto>> HandleAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<LoginResponseDto>.Failure(GenericFailureMessage);
        }

        var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
            return Result<LoginResponseDto>.Failure(GenericFailureMessage);
        }

        var (token, expiresAtUtc) = _jwtTokenService.IssueToken(user);

        user.LastLoginAt = _dateTimeProvider.UtcNow;
        user.LastActivityAt = _dateTimeProvider.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            Email = user.Email
        });
    }
}
