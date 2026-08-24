using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Queries;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Patients;

public class GetPatientByIdQueryHandlerTests
{
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    public GetPatientByIdQueryHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private GetPatientByIdQueryHandler CreateHandler() => new(_patientRepository.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task ExistingId_ReturnsDtoWithRecomputedAge()
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Doe",
            DateOfBirth = new DateOnly(1990, 5, 15),
            Gender = "Female",
            PhoneNumber = "555-123-4567",
            CreatedAt = FixedUtcNow,
            UpdatedAt = FixedUtcNow
        };
        _patientRepository.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(patient.Id);

        Assert.NotNull(result);
        Assert.Equal("Jane Doe", result!.FullName);
        Assert.Equal(36, result.Age);
    }

    [Fact]
    public async Task NonExistentId_ReturnsNull()
    {
        _patientRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
