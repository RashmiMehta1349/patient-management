using Microsoft.Extensions.Options;
using Moq;
using PatientMgmt.BusinessLogic.Auth;
using PatientMgmt.DataAccess.Repositories;
using PatientMgmt.Domain.Entities;
using PatientMgmt.Domain.Options;
using Xunit;

namespace PatientMgmt.BusinessLogic.Tests.Auth
{
    public class PasswordResetServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IPasswordResetTokenRepository> _tokenRepo = new();
        private readonly Mock<ISessionRepository> _sessionRepo = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly Mock<ITokenHasher> _tokenHasher = new();
        private readonly Mock<IEmailSender> _emailSender = new();
        private readonly FakeClock _clock = new();
        private readonly AuthOptions _authOptions = new() { ResetTokenLifetimeMinutes = 30 };
        private readonly SmtpOptions _smtpOptions = new() { FrontendBaseUrl = "https://localhost:4200" };

        private PasswordResetService CreateSut() => new(
            _userRepo.Object, _tokenRepo.Object, _sessionRepo.Object,
            _passwordHasher.Object, _tokenHasher.Object, _emailSender.Object, _clock,
            Options.Create(_authOptions), Options.Create(_smtpOptions));

        [Fact]
        public async Task RequestResetAsync_KnownEmail_CreatesTokenAndSendsEmail()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "doctor@example.com" };
            _userRepo.Setup(r => r.GetByEmailAsync("doctor@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _tokenHasher.Setup(t => t.Hash(It.IsAny<string>())).Returns("hashed-token");

            var sut = CreateSut();
            await sut.RequestResetAsync("doctor@example.com");

            _tokenRepo.Verify(t => t.CreateAsync(
                It.Is<PasswordResetToken>(pt => pt.UserId == user.Id && pt.TokenHash == "hashed-token"),
                It.IsAny<CancellationToken>()), Times.Once);
            _emailSender.Verify(e => e.SendPasswordResetEmailAsync(
                "doctor@example.com", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RequestResetAsync_UnknownEmail_DoesNotCreateTokenOrSendEmail_NoEnumerationSignal()
        {
            _userRepo.Setup(r => r.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var sut = CreateSut();
            await sut.RequestResetAsync("nobody@example.com");

            _tokenRepo.Verify(t => t.CreateAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
            _emailSender.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteResetAsync_ExpiredToken_Rejected()
        {
            var token = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TokenHash = "hashed",
                ExpiresAt = _clock.UtcNow.AddMinutes(-1), // already expired
                UsedAt = null
            };
            _tokenHasher.Setup(t => t.Hash("raw-token")).Returns("hashed");
            _tokenRepo.Setup(t => t.GetByTokenHashAsync("hashed", It.IsAny<CancellationToken>())).ReturnsAsync(token);

            var sut = CreateSut();
            var result = await sut.CompleteResetAsync("raw-token", "NewPassword1!");

            Assert.False(result.Success);
            _userRepo.Verify(r => r.UpdatePasswordHashAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteResetAsync_AlreadyUsedToken_Rejected()
        {
            var token = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TokenHash = "hashed",
                ExpiresAt = _clock.UtcNow.AddMinutes(10),
                UsedAt = _clock.UtcNow.AddMinutes(-5) // already used
            };
            _tokenHasher.Setup(t => t.Hash("raw-token")).Returns("hashed");
            _tokenRepo.Setup(t => t.GetByTokenHashAsync("hashed", It.IsAny<CancellationToken>())).ReturnsAsync(token);

            var sut = CreateSut();
            var result = await sut.CompleteResetAsync("raw-token", "NewPassword1!");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CompleteResetAsync_ValidToken_UpdatesHashMarksUsedAndInvalidatesAllSessions()
        {
            var userId = Guid.NewGuid();
            var token = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = "hashed",
                ExpiresAt = _clock.UtcNow.AddMinutes(10),
                UsedAt = null
            };
            _tokenHasher.Setup(t => t.Hash("raw-token")).Returns("hashed");
            _tokenRepo.Setup(t => t.GetByTokenHashAsync("hashed", It.IsAny<CancellationToken>())).ReturnsAsync(token);
            _passwordHasher.Setup(h => h.Hash("NewPassword1!")).Returns("new-hash");

            var sut = CreateSut();
            var result = await sut.CompleteResetAsync("raw-token", "NewPassword1!");

            Assert.True(result.Success);
            _userRepo.Verify(r => r.UpdatePasswordHashAsync(userId, "new-hash", It.IsAny<CancellationToken>()), Times.Once);
            _tokenRepo.Verify(t => t.MarkUsedAsync(token.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
            _sessionRepo.Verify(s => s.InvalidateAllForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RequestResetAsync_TokenIsUniqueAndRandomAcrossCalls()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "doctor@example.com" };
            _userRepo.Setup(r => r.GetByEmailAsync("doctor@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

            var capturedHashes = new List<string>();
            _tokenHasher.Setup(t => t.Hash(It.IsAny<string>()))
                .Returns((string raw) => raw); // pass-through so we can inspect uniqueness of raw tokens

            _tokenRepo.Setup(t => t.CreateAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
                .Callback<PasswordResetToken, CancellationToken>((t, _) => capturedHashes.Add(t.TokenHash))
                .ReturnsAsync((PasswordResetToken t, CancellationToken _) => t);

            var sut = CreateSut();
            await sut.RequestResetAsync("doctor@example.com");
            await sut.RequestResetAsync("doctor@example.com");

            Assert.Equal(2, capturedHashes.Count);
            Assert.NotEqual(capturedHashes[0], capturedHashes[1]);
        }
    }
}
