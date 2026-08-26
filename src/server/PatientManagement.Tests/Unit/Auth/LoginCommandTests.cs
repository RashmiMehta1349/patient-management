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

public class LoginCommandTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasherService> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private LoginCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _passwordHasher.Object,
        _jwtTokenService.Object,
        _dateTimeProvider.Object,
        NullLogger<LoginCommandHandler>.Instance);

    [Fact]
    public async Task ValidCredentials_ReturnsSuccessWithToken()
    {
        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Email = "doc@example.com", PasswordHash = "hashed", SecurityStamp = "stamp" };
        _userRepository.Setup(r => r.GetByEmailAsync("doc@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("hashed", "correct-password")).Returns(true);
        _jwtTokenService.Setup(j => j.IssueToken(user)).Returns(("jwt-token", DateTime.UtcNow.AddMinutes(20)));
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new LoginRequestDto { Email = "doc@example.com", Password = "correct-password" });

        Assert.True(result.Succeeded);
        Assert.Equal("jwt-token", result.Value!.Token);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WrongPassword_ReturnsGenericFailure()
    {
        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Email = "doc@example.com", PasswordHash = "hashed", SecurityStamp = "stamp" };
        _userRepository.Setup(r => r.GetByEmailAsync("doc@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("hashed", "wrong-password")).Returns(false);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new LoginRequestDto { Email = "doc@example.com", Password = "wrong-password" });

        Assert.False(result.Succeeded);
        Assert.Equal(LoginCommandHandler.GenericFailureMessage, result.Error);
    }

    [Fact]
    public async Task UnknownEmail_ReturnsSameGenericFailureAsWrongPassword()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("unknown@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new LoginRequestDto { Email = "unknown@example.com", Password = "whatever" });

        Assert.False(result.Succeeded);
        Assert.Equal(LoginCommandHandler.GenericFailureMessage, result.Error);
    }

    [Fact]
    public async Task EmptyCredentials_ReturnsGenericFailureWithoutRepositoryLookup()
    {
        var handler = CreateHandler();
        var result = await handler.HandleAsync(new LoginRequestDto { Email = "", Password = "" });

        Assert.False(result.Succeeded);
        _userRepository.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
