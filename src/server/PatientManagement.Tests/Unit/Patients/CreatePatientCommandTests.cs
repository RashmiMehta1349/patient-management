using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Commands;
using PatientManagement.Application.Patients.Dtos;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Patients;

public class CreatePatientCommandTests
{
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    public CreatePatientCommandTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private CreatePatientCommandHandler CreateHandler() => new(_patientRepository.Object, _dateTimeProvider.Object);

    private static CreatePatientRequestDto ValidRequest() => new()
    {
        FullName = "Jane Doe",
        DateOfBirth = "1990-05-15",
        Gender = "Female",
        PhoneNumber = "555-123-4567"
    };

    [Fact]
    public async Task ValidInput_ReturnsSuccessAndPersistsPatient()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(ValidRequest());

        Assert.True(result.Succeeded);
        Assert.Equal("Jane Doe", result.Value!.FullName);
        Assert.Equal("Female", result.Value.Gender);
        _patientRepository.Verify(r => r.AddAsync(It.Is<Patient>(p => p.FullName == "Jane Doe"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MissingName_ReturnsFailure()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.FullName = "";

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingDateOfBirth_ReturnsFailure()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.DateOfBirth = "";

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingGender_ReturnsFailure()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.Gender = "";

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingPhoneNumber_ReturnsFailure()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.PhoneNumber = "";

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DateOfBirthInTheFuture_ReturnsFailure()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.DateOfBirth = "2030-01-01";

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvalidGenderValue_ReturnsFailure()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.Gender = "Unknown";

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
