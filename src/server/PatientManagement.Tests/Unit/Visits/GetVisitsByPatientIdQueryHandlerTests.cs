using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Queries;
using PatientManagement.Application.Visits.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Visits;

public class GetVisitsByPatientIdQueryHandlerTests
{
    private readonly Mock<IVisitRepository> _visitRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();

    [Fact]
    public async Task ReturnsOnlyRequestedPatientsVisits_MostRecentFirst()
    {
        var patientId = Guid.NewGuid();
        var newer = new Visit { Id = Guid.NewGuid(), PatientId = patientId, VisitDate = new DateTime(2026, 8, 25), TemperatureNotRecorded = true, BloodPressureNotRecorded = true, PulseNotRecorded = true };
        var older = new Visit { Id = Guid.NewGuid(), PatientId = patientId, VisitDate = new DateTime(2026, 8, 20), TemperatureNotRecorded = true, BloodPressureNotRecorded = true, PulseNotRecorded = true };

        _visitRepository
            .Setup(r => r.GetByPatientIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit> { newer, older });
        _patientRepository
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var handler = new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(patientId);

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public async Task EmptyPatient_ReturnsEmptyArray()
    {
        var patientId = Guid.NewGuid();
        _visitRepository
            .Setup(r => r.GetByPatientIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit>());

        var handler = new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(patientId);

        Assert.Empty(result);
    }
}
