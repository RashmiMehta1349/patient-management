using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.DataExport.Queries;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Queries;
using PatientManagement.Application.Visits.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.DataExport;

public class GetVisitExportQueryHandlerTests
{
    private readonly Mock<IVisitRepository> _visitRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();

    private GetVisitExportQueryHandler CreateHandler() =>
        new(new GetVisitByIdQueryHandler(_visitRepository.Object, _patientRepository.Object));

    [Fact]
    public async Task UnknownVisit_ReturnsNull()
    {
        _visitRepository.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Visit?)null);

        var result = await CreateHandler().HandleAsync(Random.Shared.NextInt64(1, long.MaxValue));

        Assert.Null(result);
    }

    [Fact]
    public async Task KnownVisit_MapsAllVitalsStatesAndComplaints()
    {
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        var visit = new Visit
        {
            Id = Random.Shared.NextInt64(1, long.MaxValue),
            PatientId = patientId,
            VisitDate = new DateTime(2026, 8, 25),
            Complaints = "Fever and cough",
            Diagnosis = "Viral infection",
            TemperatureNotRecorded = true,
            BloodPressureNotRecorded = true,
            PulseNotRecorded = true
        };
        _visitRepository.Setup(r => r.GetByIdAsync(visit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(visit);
        _patientRepository.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var result = await CreateHandler().HandleAsync(visit.Id);

        Assert.NotNull(result);
        Assert.Equal("Jane Doe", result!.PatientName);
        Assert.Equal("Fever and cough", result.Complaints);
        Assert.Equal("Viral infection", result.Diagnosis);
        Assert.True(result.TemperatureNotRecorded);
        Assert.True(result.BloodPressureNotRecorded);
        Assert.True(result.PulseNotRecorded);
        Assert.Empty(result.Medications);
    }

    [Fact]
    public async Task VisitWithMedications_MapsAllMedicationLines()
    {
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        var visit = new Visit
        {
            Id = Random.Shared.NextInt64(1, long.MaxValue),
            PatientId = patientId,
            VisitDate = DateTime.UtcNow,
            Medications = new System.Collections.Generic.List<Medication>
            {
                new() { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = "Paracetamol", Dosage = "500mg", Frequency = "Twice daily", Duration = "5 days", Instructions = "After food", SortOrder = 0 }
            }
        };
        _visitRepository.Setup(r => r.GetByIdAsync(visit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(visit);
        _patientRepository.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "John Smith", Gender = "Male", PhoneNumber = "555", DateOfBirth = new DateOnly(1980, 1, 1) });

        var result = await CreateHandler().HandleAsync(visit.Id);

        Assert.NotNull(result);
        Assert.Single(result!.Medications);
        Assert.Equal("Paracetamol", result.Medications[0].Name);
    }
}
