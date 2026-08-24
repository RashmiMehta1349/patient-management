using Microsoft.Extensions.Options;
using Moq;
using PatientMgmt.BusinessLogic.Auth;
using PatientMgmt.DataAccess.Repositories;
using PatientMgmt.Domain.Entities;
using PatientMgmt.Domain.Options;
using Xunit;

namespace PatientMgmt.BusinessLogic.Tests.Auth
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<ISessionRepository> _sessionRepo = new();
        private readonly Mock<IPasswordHasher> _hasher = new();
        private readonly Mock<IJwtTokenService> _jwt = new();
        private readonly FakeClock _clock = new();
        private readonly AuthOptions _options = new() { IdleTimeoutMinutes = 15, SessionHardExpiryMinutes = 720 };

        private AuthService CreateSut() =>
            new(_userRepo.Object, _sessionRepo.Object, _hasher.Object, _jwt.Object, _clock, Options.Create(_options));

        private static User MakeUser(string password, IPasswordHasher realHasher) => new()
        {
            Id = Guid.NewGuid(),
            Email = "doctor@example.com",
            Username = "doctor",
            PasswordHash = realHasher.Hash(password),
            CreatedAt = DateTime.UtcNow
        };

        [Fact]
        public async Task LoginAsync_CorrectCredentials_CreatesSessionAndIssuesToken()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "doctor@example.com", PasswordHash = "hash" };
            _userRepo.Setup(r => r.GetByUsernameOrEmailAsync("doctor@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("Password1!", "hash")).Returns(true);
            _jwt.Setup(j => j.IssueToken(user.Id, user.Email, It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .Returns("fake-jwt");

            var sut = CreateSut();
            var result = await sut.LoginAsync("doctor@example.com", "Password1!");

            Assert.True(result.Success);
            Assert.Equal("fake-jwt", result.AccessToken);
            _sessionRepo.Verify(s => s.CreateAsync(It.Is<Session>(sess => sess.UserId == user.Id), It.IsAny<CancellationToken>()), Times.Once);
            _userRepo.Verify(r => r.UpdateLastLoginAsync(user.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_IncorrectPassword_ReturnsGenericFailure()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "doctor@example.com", PasswordHash = "hash" };
            _userRepo.Setup(r => r.GetByUsernameOrEmailAsync("doctor@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _hasher.Setup(h => h.Verify("WrongPassword", "hash")).Returns(false);

            var sut = CreateSut();
            var result = await sut.LoginAsync("doctor@example.com", "WrongPassword");

            Assert.False(result.Success);
            Assert.Null(result.AccessToken);
            _sessionRepo.Verify(s => s.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_UnknownUser_ReturnsSameGenericFailureAsBadPassword()
        {
            _userRepo.Setup(r => r.GetByUsernameOrEmailAsync("nobody@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var sut = CreateSut();
            var result = await sut.LoginAsync("nobody@example.com", "whatever");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ValidateSessionAsync_JustUnderIdleThreshold_IsValid()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "doctor@example.com" };
            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                IsValid = true,
                LastActivityAt = _clock.UtcNow.AddMinutes(-14), // just under 15 min idle timeout
                ExpiresAt = _clock.UtcNow.AddHours(1)
            };
            _sessionRepo.Setup(s => s.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
            _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

            var sut = CreateSut();
            var result = await sut.ValidateSessionAsync(session.Id);

            Assert.True(result.IsValid);
            _sessionRepo.Verify(s => s.UpdateLastActivityAsync(session.Id, _clock.UtcNow, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ValidateSessionAsync_JustOverIdleThreshold_IsInvalid()
        {
            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                IsValid = true,
                LastActivityAt = _clock.UtcNow.AddMinutes(-16), // just over 15 min idle timeout
                ExpiresAt = _clock.UtcNow.AddHours(1)
            };
            _sessionRepo.Setup(s => s.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

            var sut = CreateSut();
            var result = await sut.ValidateSessionAsync(session.Id);

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ValidateSessionAsync_InvalidatedSession_IsInvalid()
        {
            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                IsValid = false, // e.g., after logout or password reset
                LastActivityAt = _clock.UtcNow,
                ExpiresAt = _clock.UtcNow.AddHours(1)
            };
            _sessionRepo.Setup(s => s.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

            var sut = CreateSut();
            var result = await sut.ValidateSessionAsync(session.Id);

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task LogoutAsync_InvalidatesSession()
        {
            var sessionId = Guid.NewGuid();
            var sut = CreateSut();

            await sut.LogoutAsync(sessionId);

            _sessionRepo.Verify(s => s.InvalidateAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
