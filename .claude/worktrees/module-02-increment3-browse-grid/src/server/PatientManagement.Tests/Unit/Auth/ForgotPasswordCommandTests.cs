using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Commands;
using PatientManagement.Application.Auth.Dtos;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Auth;

public class ForgotPasswordCommandTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _resetTokenRepository = new();
    private readonly Mock<IResetTokenGenerator> _tokenGenerator = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private ForgotPasswordCommandHandler CreateHandler()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);
        _tokenGenerator.Setup(t => t.GenerateRawToken()).Returns("raw-token");
        _tokenGenerator.Setup(t => t.HashToken(It.IsAny<string>())).Returns("hashed-token");
        var options = Options.Create(new AuthOptions());
        return new ForgotPasswordCommandHandler(
            _userRepository.Object,
            _resetTokenRepository.Object,
            _tokenGenerator.Object,
            _emailSender.Object,
            _dateTimeProvider.Object,
            options,
            NullLogger<ForgotPasswordCommandHandler>.Instance);
    }

    [Fact]
    public async Task MatchingEmail_GeneratesTokenAndSendsEmail()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "doc@example.com" };
        _userRepository.Setup(r => r.GetByEmailAsync("doc@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var handler = CreateHandler();
        var message = await handler.HandleAsync(new ForgotPasswordRequestDto { Email = "doc@example.com" });

        Assert.Equal(ForgotPasswordCommandHandler.GenericResponseMessage, message);
        _resetTokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(e => e.SendPasswordResetEmailAsync("doc@example.com", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NonMatchingEmail_ReturnsSameGenericResponseWithoutCreatingToken()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var message = await handler.HandleAsync(new ForgotPasswordRequestDto { Email = "nobody@example.com" });

        Assert.Equal(ForgotPasswordCommandHandler.GenericResponseMessage, message);
        _resetTokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
