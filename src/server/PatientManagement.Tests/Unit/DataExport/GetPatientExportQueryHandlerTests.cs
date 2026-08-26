using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Appointments.Queries;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.DataExport.Queries;
using PatientManagement.Application.Patients.Queries;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Queries;
using PatientManagement.Application.Visits.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.DataExport;

public class GetPatientExportQueryHandlerTests
{
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IVisitRepository> _visitRepository = new();
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    public GetPatientExportQueryHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(new DateTime(2026, 8, 25));
        _appointmentRepository.Setup(r => r.GetByPatientIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Appointment>());
    }

    private GetPatientExportQueryHandler CreateHandler() =>
        new(
            new GetPatientByIdQueryHandler(_patientRepository.Object, _dateTimeProvider.Object),
            new GetVisitsByPatientIdQueryHandler(_visitRepository.Object, _patientRepository.Object),
            new GetAppointmentsByPatientIdQueryHandler(_appointmentRepository.Object, _patientRepository.Object));

    [Fact]
    public async Task UnknownPatient_ReturnsNull()
    {
        _patientRepository.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

        var result = await CreateHandler().HandleAsync(Random.Shared.NextInt64(1, long.MaxValue), includeHistory: true);

        Assert.Null(result);
    }

    [Fact]
    public async Task IncludeHistoryOmitted_HistorySectionIsNull()
    {
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        _patientRepository.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1), CreatedAt = new DateTime(2024, 1, 1) });

        var result = await CreateHandler().HandleAsync(patientId);

        Assert.NotNull(result);
        Assert.Null(result!.VisitSummaries);
        _visitRepository.Verify(r => r.GetByPatientIdAsync(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IncludeHistoryTrue_ZeroVisits_ReturnsEmptyHistoryNotError()
    {
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        _patientRepository.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1), CreatedAt = new DateTime(2024, 1, 1) });
        _visitRepository.Setup(r => r.GetByPatientIdAsync(patientId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit>());

        var result = await CreateHandler().HandleAsync(patientId, includeHistory: true);

        Assert.NotNull(result);
        Assert.NotNull(result!.VisitSummaries);
        Assert.Empty(result.VisitSummaries!);
    }

    [Fact]
    public async Task IncludeHistoryTrue_SummarizesVisitsWithFullMedicationDetail()
    {
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        _patientRepository.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1), CreatedAt = new DateTime(2024, 1, 1) });
        _visitRepository.Setup(r => r.GetByPatientIdAsync(patientId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Visit>
            {
                new()
                {
                    Id = Random.Shared.NextInt64(1, long.MaxValue),
                    PatientId = patientId,
                    VisitDate = new DateTime(2026, 8, 20),
                    Diagnosis = "Flu",
                    TemperatureNotRecorded = true,
                    BloodPressureValue = "120/80",
                    PulseValue = 72,
                    Medications = new List<Medication>
                    {
                        new() { Name = "Paracetamol", Dosage = "500mg", Frequency = "Twice daily", Duration = "5 days", Instructions = "After food" },
                        new() { Name = "Cough Syrup" }
                    }
                }
            });

        var result = await CreateHandler().HandleAsync(patientId, includeHistory: true);

        Assert.NotNull(result!.VisitSummaries);
        var summary = Assert.Single(result.VisitSummaries!);
        Assert.Equal("Flu", summary.Diagnosis);
        Assert.Equal(2, summary.Medications.Count);
        Assert.Equal("Paracetamol", summary.Medications[0].Name);
        Assert.Equal("500mg", summary.Medications[0].Dosage);
        Assert.Equal("Twice daily", summary.Medications[0].Frequency);
        Assert.Equal("5 days", summary.Medications[0].Duration);
        Assert.Equal("After food", summary.Medications[0].Instructions);
        Assert.Equal("Cough Syrup", summary.Medications[1].Name);
        Assert.Null(summary.Medications[1].Dosage);
        Assert.Equal("Not recorded", summary.TemperatureDisplay);
        Assert.Equal("120/80", summary.BloodPressureDisplay);
        Assert.Equal("72", summary.PulseDisplay);
    }
}
