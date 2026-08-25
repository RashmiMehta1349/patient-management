using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Commands;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Application.Visits.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Visits;

public class UpdateVisitCommandTests
{
    private readonly Mock<IVisitRepository> _visitRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    public UpdateVisitCommandTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private UpdateVisitCommandHandler CreateHandler() =>
        new(_visitRepository.Object, _patientRepository.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task ValidEdit_PersistsChanges()
    {
        var patientId = Guid.NewGuid();
        var visit = new Visit { Id = Guid.NewGuid(), PatientId = patientId, VisitDate = new DateTime(2026, 8, 20), TemperatureNotRecorded = true, BloodPressureNotRecorded = true, PulseNotRecorded = true };
        _visitRepository.Setup(r => r.GetByIdAsync(visit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(visit);
        _patientRepository
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var handler = CreateHandler();
        var request = new UpdateVisitRequestDto
        {
            TemperatureValue = 100.4m,
            TemperatureNotRecorded = false,
            BloodPressureValue = "130/85",
            BloodPressureNotRecorded = false,
            PulseValue = 80,
            PulseNotRecorded = false,
            Complaints = "Headache",
            Diagnosis = "Migraine"
        };

        var result = await handler.HandleAsync(visit.Id, request);

        Assert.True(result.Succeeded);
        Assert.Equal(100.4m, result.Value!.TemperatureValue);
        Assert.Equal("130/85", result.Value.BloodPressureValue);
        Assert.Equal(80, result.Value.PulseValue);
        Assert.Equal("Headache", result.Value.Complaints);
        Assert.Equal("Migraine", result.Value.Diagnosis);
        _visitRepository.Verify(r => r.UpdateAsync(It.IsAny<Visit>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnknownId_ReturnsNotFound()
    {
        _visitRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Visit?)null);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Guid.NewGuid(), new UpdateVisitRequestDto { TemperatureNotRecorded = true, BloodPressureNotRecorded = true, PulseNotRecorded = true });

        Assert.False(result.Succeeded);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task ConflictingVitalPayload_NormalizesRatherThanFails()
    {
        var patientId = Guid.NewGuid();
        var visit = new Visit { Id = Guid.NewGuid(), PatientId = patientId, VisitDate = new DateTime(2026, 8, 20) };
        _visitRepository.Setup(r => r.GetByIdAsync(visit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(visit);
        _patientRepository
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var handler = CreateHandler();
        var request = new UpdateVisitRequestDto
        {
            PulseValue = 75,
            PulseNotRecorded = true,
            TemperatureNotRecorded = true,
            BloodPressureNotRecorded = true
        };

        var result = await handler.HandleAsync(visit.Id, request);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.PulseNotRecorded);
        Assert.Null(result.Value.PulseValue);
    }

    [Fact]
    public async Task ReplacesFullMedicationSet_RemovingOmittedRowsAndAddingNewRows()
    {
        var patientId = Guid.NewGuid();
        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            VisitDate = new DateTime(2026, 8, 20),
            TemperatureNotRecorded = true,
            BloodPressureNotRecorded = true,
            PulseNotRecorded = true,
            Medications = new List<Medication>
            {
                new() { Id = Guid.NewGuid(), Name = "OldMed", Dosage = "1", Frequency = "1", Duration = "1", Instructions = "1", SortOrder = 0 }
            }
        };
        _visitRepository.Setup(r => r.GetByIdAsync(visit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(visit);
        _patientRepository
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var handler = CreateHandler();
        var request = new UpdateVisitRequestDto
        {
            TemperatureNotRecorded = true,
            BloodPressureNotRecorded = true,
            PulseNotRecorded = true,
            Medications = new List<MedicationDto>
            {
                new() { Name = "NewMed", Dosage = "500mg", Frequency = "Once", Duration = "5 days", Instructions = "After food" }
            }
        };

        var result = await handler.HandleAsync(visit.Id, request);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.Medications);
        Assert.Equal("NewMed", result.Value.Medications[0].Name);
        _visitRepository.Verify(
            r => r.ReplaceMedicationsAsync(visit.Id, It.Is<IReadOnlyList<Medication>>(m => m.Count == 1 && m[0].Name == "NewMed"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemovingAllMedications_PersistsEmptySet()
    {
        var patientId = Guid.NewGuid();
        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            VisitDate = new DateTime(2026, 8, 20),
            TemperatureNotRecorded = true,
            BloodPressureNotRecorded = true,
            PulseNotRecorded = true,
            Medications = new List<Medication>
            {
                new() { Id = Guid.NewGuid(), Name = "OldMed", Dosage = "1", Frequency = "1", Duration = "1", Instructions = "1", SortOrder = 0 }
            }
        };
        _visitRepository.Setup(r => r.GetByIdAsync(visit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(visit);
        _patientRepository
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var handler = CreateHandler();
        var request = new UpdateVisitRequestDto
        {
            TemperatureNotRecorded = true,
            BloodPressureNotRecorded = true,
            PulseNotRecorded = true,
            Medications = new List<MedicationDto>()
        };

        var result = await handler.HandleAsync(visit.Id, request);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!.Medications);
    }

    [Fact]
    public async Task TouchedRowMissingSomeFields_Fails()
    {
        var patientId = Guid.NewGuid();
        var visit = new Visit { Id = Guid.NewGuid(), PatientId = patientId, VisitDate = new DateTime(2026, 8, 20) };
        _visitRepository.Setup(r => r.GetByIdAsync(visit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(visit);

        var handler = CreateHandler();
        var request = new UpdateVisitRequestDto
        {
            TemperatureNotRecorded = true,
            BloodPressureNotRecorded = true,
            PulseNotRecorded = true,
            Medications = new List<MedicationDto>
            {
                new() { Name = "Ibuprofen" }
            }
        };

        var result = await handler.HandleAsync(visit.Id, request);

        Assert.False(result.Succeeded);
        _visitRepository.Verify(r => r.ReplaceMedicationsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Medication>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
