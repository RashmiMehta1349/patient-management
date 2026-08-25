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
            .Setup(r => r.GetByPatientIdAsync(patientId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit> { newer, older });
        _patientRepository
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var handler = new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(patientId);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(newer.Id, result.Value[0].Id);
        Assert.Equal(older.Id, result.Value[1].Id);
    }

    [Fact]
    public async Task EmptyPatient_ReturnsEmptyArray()
    {
        var patientId = Guid.NewGuid();
        _visitRepository
            .Setup(r => r.GetByPatientIdAsync(patientId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit>());

        var handler = new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(patientId);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task BothDatesSupplied_ReturnsOnlyVisitsWithinInclusiveRange()
    {
        var patientId = Guid.NewGuid();
        var inRange = new Visit { Id = Guid.NewGuid(), PatientId = patientId, VisitDate = new DateTime(2026, 8, 10), TemperatureNotRecorded = true, BloodPressureNotRecorded = true, PulseNotRecorded = true };

        var from = new DateTime(2026, 8, 1);
        var to = new DateTime(2026, 8, 20);
        var expectedTo = to.Date.AddDays(1).AddTicks(-1);

        _visitRepository
            .Setup(r => r.GetByPatientIdAsync(patientId, from.Date, expectedTo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit> { inRange });
        _patientRepository
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var handler = new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(patientId, from, to);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
        Assert.Equal(inRange.Id, result.Value![0].Id);
    }

    [Fact]
    public async Task OnlyFromDateSupplied_PassesStartOfDayAndNullTo()
    {
        var patientId = Guid.NewGuid();
        var from = new DateTime(2026, 8, 1, 13, 0, 0);

        _visitRepository
            .Setup(r => r.GetByPatientIdAsync(patientId, from.Date, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit>());

        var handler = new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(patientId, from, null);

        Assert.True(result.Succeeded);
        _visitRepository.VerifyAll();
    }

    [Fact]
    public async Task OnlyToDateSupplied_PassesEndOfDayAndNullFrom()
    {
        var patientId = Guid.NewGuid();
        var to = new DateTime(2026, 8, 20);
        var expectedTo = to.Date.AddDays(1).AddTicks(-1);

        _visitRepository
            .Setup(r => r.GetByPatientIdAsync(patientId, null, expectedTo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit>());

        var handler = new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(patientId, null, to);

        Assert.True(result.Succeeded);
        _visitRepository.VerifyAll();
    }

    [Fact]
    public async Task RangeWithNoMatchingVisits_ReturnsEmptyArrayNotError()
    {
        var patientId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);

        _visitRepository
            .Setup(r => r.GetByPatientIdAsync(patientId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit>());

        var handler = new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(patientId, from, to);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task FromDateAfterToDate_ReturnsValidationFailure()
    {
        var patientId = Guid.NewGuid();
        var from = new DateTime(2026, 8, 20);
        var to = new DateTime(2026, 8, 1);

        var handler = new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(patientId, from, to);

        Assert.False(result.Succeeded);
        Assert.False(result.IsNotFound);
        _visitRepository.Verify(r => r.GetByPatientIdAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SingleDayRange_FromEqualsTo_ReturnsOnlyVisitsOnThatExactDate()
    {
        var patientId = Guid.NewGuid();
        var day = new DateTime(2026, 8, 10);
        var expectedTo = day.Date.AddDays(1).AddTicks(-1);
        var onThatDay = new Visit { Id = Guid.NewGuid(), PatientId = patientId, VisitDate = new DateTime(2026, 8, 10, 9, 30, 0), TemperatureNotRecorded = true, BloodPressureNotRecorded = true, PulseNotRecorded = true };

        _visitRepository
            .Setup(r => r.GetByPatientIdAsync(patientId, day.Date, expectedTo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit> { onThatDay });
        _patientRepository
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var handler = new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object);

        var result = await handler.HandleAsync(patientId, day, day);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
    }
}
