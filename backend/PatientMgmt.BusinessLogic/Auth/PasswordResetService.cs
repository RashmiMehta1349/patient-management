using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using PatientMgmt.DataAccess.Repositories;
using PatientMgmt.Domain.Entities;
using PatientMgmt.Domain.Options;

namespace PatientMgmt.BusinessLogic.Auth
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetTokenRepository _tokenRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenHasher _tokenHasher;
        private readonly IEmailSender _emailSender;
        private readonly IClock _clock;
        private readonly AuthOptions _authOptions;
        private readonly SmtpOptions _smtpOptions;

        public PasswordResetService(
            IUserRepository userRepository,
            IPasswordResetTokenRepository tokenRepository,
            ISessionRepository sessionRepository,
            IPasswordHasher passwordHasher,
            ITokenHasher tokenHasher,
            IEmailSender emailSender,
            IClock clock,
            IOptions<AuthOptions> authOptions,
            IOptions<SmtpOptions> smtpOptions)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _sessionRepository = sessionRepository;
            _passwordHasher = passwordHasher;
            _tokenHasher = tokenHasher;
            _emailSender = emailSender;
            _clock = clock;
            _authOptions = authOptions.Value;
            _smtpOptions = smtpOptions.Value;
        }

        public async Task RequestResetAsync(string email, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return; // caller returns generic success regardless

            var user = await _userRepository.GetByEmailAsync(email, ct);
            if (user is null)
                return; // no enumeration signal; silently no-op

            var rawToken = GenerateRawToken();
            var tokenHash = _tokenHasher.Hash(rawToken);
            var now = _clock.UtcNow;

            var token = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = now.AddMinutes(_authOptions.ResetTokenLifetimeMinutes),
                CreatedAt = now
            };
            await _tokenRepository.CreateAsync(token, ct);

            var resetLink = $"{_smtpOptions.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
            await _emailSender.SendPasswordResetEmailAsync(user.Email, resetLink, ct);
        }

        public async Task<ResetCompletionResult> CompleteResetAsync(string rawToken, string newPassword, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(newPassword))
                return ResetCompletionResult.Invalid();

            var tokenHash = _tokenHasher.Hash(rawToken);
            var token = await _tokenRepository.GetByTokenHashAsync(tokenHash, ct);
            if (token is null)
                return ResetCompletionResult.Invalid();

            var now = _clock.UtcNow;
            if (token.UsedAt is not null || now >= token.ExpiresAt)
                return ResetCompletionResult.Invalid();

            var newHash = _passwordHasher.Hash(newPassword);
            await _userRepository.UpdatePasswordHashAsync(token.UserId, newHash, ct);
            await _tokenRepository.MarkUsedAsync(token.Id, now, ct);
            await _sessionRepository.InvalidateAllForUserAsync(token.UserId, ct);

            return ResetCompletionResult.Ok();
        }

        private static string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
