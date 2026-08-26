using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Queries;
using PatientManagement.Application.Visits.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Visits;

public class GetVisitByIdQueryHandlerTests
{
    private readonly Mock<IVisitRepository> _visitRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();

    [Fact]
    public async Task Found_ReturnsDto()
    {
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        var visit = new Visit { Id = Random.Shared.NextInt64(1, long.MaxValue), PatientId = patientId, VisitDate = DateTime.UtcNow, TemperatureNotRecorded = true, BloodPressureNotRecorded = true, PulseNotRecorded = true };
        _visitRepository.Setup(r => r.GetByIdAsync(visit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(visit);
        _patientRepository
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var handler = new GetVisitByIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(visit.Id);

        Assert.NotNull(result);
        Assert.Equal("Jane Doe", result!.PatientName);
    }

    [Fact]
    public async Task NotFound_ReturnsNull()
    {
        _visitRepository.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Visit?)null);

        var handler = new GetVisitByIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(Random.Shared.NextInt64(1, long.MaxValue));

        Assert.Null(result);
    }
}
