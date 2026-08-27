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

public class UpdatePatientCommandTests
{
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime OriginalUtc = new(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedUtcNow = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    public UpdatePatientCommandTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private UpdatePatientCommandHandler CreateHandler() => new(_patientRepository.Object, _dateTimeProvider.Object);

    private static Patient ExistingPatient(long id) => new()
    {
        Id = id,
        FullName = "Jane Doe",
        DateOfBirth = new DateOnly(1990, 5, 15),
        Gender = "Female",
        CountryCode = "+91",
        PhoneNumber = "9876543210",
        CreatedAt = OriginalUtc,
        UpdatedAt = OriginalUtc
    };

    private static UpdatePatientRequestDto ValidRequest() => new()
    {
        FullName = "Jane A. Doe",
        DateOfBirth = "1990-05-15",
        Gender = "Female",
        CountryCode = "+91",
        PhoneNumber = "9999000000"
    };

    [Fact]
    public async Task ValidChanges_UpdatesAndReturnsSuccess()
    {
        var id = Random.Shared.NextInt64(1, long.MaxValue);
        var patient = ExistingPatient(id);
        _patientRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(id, ValidRequest());

        Assert.True(result.Succeeded);
        Assert.Equal("Jane A. Doe", result.Value!.FullName);
        Assert.Equal("9999000000", result.Value.PhoneNumber);
        Assert.Equal(id, result.Value.Id);
        Assert.True(patient.UpdatedAt > OriginalUtc);
        Assert.Equal(OriginalUtc, patient.CreatedAt);
        _patientRepository.Verify(r => r.UpdateAsync(patient, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MissingName_ReturnsFailure()
    {
        var id = Random.Shared.NextInt64(1, long.MaxValue);
        _patientRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingPatient(id));
        var request = ValidRequest();
        request.FullName = "";

        var result = await CreateHandler().HandleAsync(id, request);

        Assert.False(result.Succeeded);
        Assert.False(result.IsNotFound);
        _patientRepository.Verify(r => r.UpdateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingDateOfBirth_ReturnsFailure()
    {
        var id = Random.Shared.NextInt64(1, long.MaxValue);
        _patientRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingPatient(id));
        var request = ValidRequest();
        request.DateOfBirth = "";

        var result = await CreateHandler().HandleAsync(id, request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.UpdateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingGender_ReturnsFailure()
    {
        var id = Random.Shared.NextInt64(1, long.MaxValue);
        _patientRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingPatient(id));
        var request = ValidRequest();
        request.Gender = "";

        var result = await CreateHandler().HandleAsync(id, request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.UpdateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingPhoneNumber_ReturnsFailure()
    {
        var id = Random.Shared.NextInt64(1, long.MaxValue);
        _patientRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingPatient(id));
        var request = ValidRequest();
        request.PhoneNumber = "";

        var result = await CreateHandler().HandleAsync(id, request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.UpdateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DateOfBirthInTheFuture_ReturnsFailure()
    {
        var id = Random.Shared.NextInt64(1, long.MaxValue);
        _patientRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingPatient(id));
        var request = ValidRequest();
        request.DateOfBirth = "2030-01-01";

        var result = await CreateHandler().HandleAsync(id, request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.UpdateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvalidGenderValue_ReturnsFailure()
    {
        var id = Random.Shared.NextInt64(1, long.MaxValue);
        _patientRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingPatient(id));
        var request = ValidRequest();
        request.Gender = "Unknown";

        var result = await CreateHandler().HandleAsync(id, request);

        Assert.False(result.Succeeded);
        _patientRepository.Verify(r => r.UpdateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NonExistentId_ReturnsNotFoundResult()
    {
        _patientRepository.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

        var result = await CreateHandler().HandleAsync(Random.Shared.NextInt64(1, long.MaxValue), ValidRequest());

        Assert.False(result.Succeeded);
        Assert.True(result.IsNotFound);
        _patientRepository.Verify(r => r.UpdateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
