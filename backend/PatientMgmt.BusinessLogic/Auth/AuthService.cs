using Microsoft.Extensions.Options;
using PatientMgmt.DataAccess.Repositories;
using PatientMgmt.Domain.Entities;
using PatientMgmt.Domain.Options;

namespace PatientMgmt.BusinessLogic.Auth
{
    /// <summary>
    /// Business rules: correct credentials required for login; generic failure on either
    /// unknown user or bad password (no enumeration); idle-timeout + hard-expiry enforcement
    /// on every session validation; logout invalidates the session server-side.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IClock _clock;
        private readonly AuthOptions _options;

        public AuthService(
            IUserRepository userRepository,
            ISessionRepository sessionRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IClock clock,
            IOptions<AuthOptions> options)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _clock = clock;
            _options = options.Value;
        }

        public async Task<LoginResult> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                return LoginResult.Fail();

            var user = await _userRepository.GetByUsernameOrEmailAsync(usernameOrEmail, ct);
            if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
            {
                // Generic failure regardless of which check failed (Business Rule / Acceptance Criteria).
                return LoginResult.Fail();
            }

            var now = _clock.UtcNow;
            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                IssuedAt = now,
                LastActivityAt = now,
                IsValid = true,
                ExpiresAt = now.AddMinutes(_options.SessionHardExpiryMinutes)
            };
            await _sessionRepository.CreateAsync(session, ct);
            await _userRepository.UpdateLastLoginAsync(user.Id, now, ct);

            var token = _jwtTokenService.IssueToken(user.Id, user.Email, session.Id, session.ExpiresAt);
            return LoginResult.Ok(token, session.ExpiresAt);
        }

        public async Task LogoutAsync(Guid sessionId, CancellationToken ct = default)
        {
            await _sessionRepository.InvalidateAsync(sessionId, ct);
        }

        public async Task<SessionValidationResult> ValidateSessionAsync(Guid sessionId, CancellationToken ct = default)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, ct);
            if (session is null || !session.IsValid)
                return SessionValidationResult.Invalid();

            var now = _clock.UtcNow;
            if (now >= session.ExpiresAt)
                return SessionValidationResult.Invalid();

            var idleDeadline = session.LastActivityAt.AddMinutes(_options.IdleTimeoutMinutes);
            if (now > idleDeadline)
                return SessionValidationResult.Invalid();

            var user = await _userRepository.GetByIdAsync(session.UserId, ct);
            if (user is null)
                return SessionValidationResult.Invalid();

            await _sessionRepository.UpdateLastActivityAsync(session.Id, now, ct);
            return SessionValidationResult.Valid(user.Id, user.Email);
        }
    }
}
