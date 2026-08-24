using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PatientManagement.Application.Auth.Commands;
using PatientManagement.Application.Auth.Dtos;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Auth;

public class ResetPasswordCommandTests
{
    private readonly Mock<IPasswordResetTokenRepository> _resetTokenRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasherService> _passwordHasher = new();
    private readonly Mock<IResetTokenGenerator> _tokenGenerator = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private ResetPasswordCommandHandler CreateHandler()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(Now);
        _tokenGenerator.Setup(t => t.HashToken(It.IsAny<string>())).Returns((string raw) => $"hash-of-{raw}");
        return new ResetPasswordCommandHandler(
            _resetTokenRepository.Object,
            _userRepository.Object,
            _passwordHasher.Object,
            _tokenGenerator.Object,
            _dateTimeProvider.Object,
            NullLogger<ResetPasswordCommandHandler>.Instance);
    }

    [Fact]
    public async Task ValidUnexpiredUnconsumedToken_UpdatesPasswordAndRegeneratesSecurityStamp()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "doc@example.com", PasswordHash = "old-hash", SecurityStamp = "old-stamp" };
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash-of-raw-token",
            ExpiresAt = Now.AddMinutes(10),
            ConsumedAt = null
        };

        _resetTokenRepository.Setup(r => r.GetByTokenHashAsync("hash-of-raw-token", It.IsAny<CancellationToken>())).ReturnsAsync(resetToken);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.HashPassword("NewPassword123!")).Returns("new-hash");

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new ResetPasswordRequestDto { Token = "raw-token", NewPassword = "NewPassword123!" });

        Assert.True(result.Succeeded);
        Assert.Equal("new-hash", user.PasswordHash);
        Assert.NotEqual("old-stamp", user.SecurityStamp);
        Assert.Equal(Now, resetToken.ConsumedAt);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _resetTokenRepository.Verify(r => r.UpdateAsync(resetToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash-of-raw-token",
            ExpiresAt = Now.AddMinutes(-1),
            ConsumedAt = null
        };
        _resetTokenRepository.Setup(r => r.GetByTokenHashAsync("hash-of-raw-token", It.IsAny<CancellationToken>())).ReturnsAsync(resetToken);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new ResetPasswordRequestDto { Token = "raw-token", NewPassword = "NewPassword123!" });

        Assert.False(result.Succeeded);
        Assert.Equal(ResetPasswordCommandHandler.InvalidOrExpiredTokenMessage, result.Error);
    }

    [Fact]
    public async Task AlreadyConsumedToken_IsRejected()
    {
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash-of-raw-token",
            ExpiresAt = Now.AddMinutes(10),
            ConsumedAt = Now.AddMinutes(-5)
        };
        _resetTokenRepository.Setup(r => r.GetByTokenHashAsync("hash-of-raw-token", It.IsAny<CancellationToken>())).ReturnsAsync(resetToken);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new ResetPasswordRequestDto { Token = "raw-token", NewPassword = "NewPassword123!" });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task MalformedOrUnknownToken_IsRejected()
    {
        _resetTokenRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((PasswordResetToken?)null);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new ResetPasswordRequestDto { Token = "garbage", NewPassword = "NewPassword123!" });

        Assert.False(result.Succeeded);
    }
}
