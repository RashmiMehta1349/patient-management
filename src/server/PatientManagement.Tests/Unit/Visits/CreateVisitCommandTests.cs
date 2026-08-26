using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Commands;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Application.Visits.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Visits;

public class CreateVisitCommandTests
{
    private readonly Mock<IVisitRepository> _visitRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly long ExistingPatientId = Random.Shared.NextInt64(1, long.MaxValue);
    private static readonly long OtherPatientId = Random.Shared.NextInt64(1, long.MaxValue);

    public CreateVisitCommandTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
        _patientRepository
            .Setup(r => r.GetByIdAsync(ExistingPatientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = ExistingPatientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });
    }

    private CreateVisitCommandHandler CreateHandler() =>
        new(_visitRepository.Object, _patientRepository.Object, _appointmentRepository.Object, _dateTimeProvider.Object);

    private static CreateVisitRequestDto ValidRequest() => new()
    {
        PatientId = ExistingPatientId,
        TemperatureNotRecorded = true,
        BloodPressureNotRecorded = true,
        PulseNotRecorded = true
    };

    [Fact]
    public async Task AllVitalsRecordedAsValues_Succeeds()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.TemperatureValue = 98.6m;
        request.TemperatureNotRecorded = false;
        request.BloodPressureValue = "120/80";
        request.BloodPressureNotRecorded = false;
        request.PulseValue = 72;
        request.PulseNotRecorded = false;

        var result = await handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(98.6m, result.Value!.TemperatureValue);
        Assert.Equal("120/80", result.Value.BloodPressureValue);
        Assert.Equal(72, result.Value.PulseValue);
        _visitRepository.Verify(r => r.AddAsync(It.IsAny<Visit>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AllVitalsNotRecorded_Succeeds()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(ValidRequest());

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.TemperatureNotRecorded);
        Assert.True(result.Value.BloodPressureNotRecorded);
        Assert.True(result.Value.PulseNotRecorded);
    }

    [Fact]
    public async Task MixedVitals_Succeeds()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.TemperatureValue = 99.1m;
        request.TemperatureNotRecorded = false;
        // BP and Pulse left as not recorded

        var result = await handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(99.1m, result.Value!.TemperatureValue);
        Assert.True(result.Value.BloodPressureNotRecorded);
        Assert.True(result.Value.PulseNotRecorded);
    }

    [Fact]
    public async Task MissingPatient_ReturnsNotFound()
    {
        _patientRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);
        var handler = CreateHandler();
        var request = ValidRequest();
        request.PatientId = Random.Shared.NextInt64(1, long.MaxValue);

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        Assert.True(result.IsNotFound);
        _visitRepository.Verify(r => r.AddAsync(It.IsAny<Visit>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnknownAppointmentId_ReturnsNotFound()
    {
        _appointmentRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);
        var handler = CreateHandler();
        var request = ValidRequest();
        request.AppointmentId = Random.Shared.NextInt64(1, long.MaxValue);

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task MismatchedAppointmentPatient_ReturnsFailure()
    {
        var appointmentId = Random.Shared.NextInt64(1, long.MaxValue);
        _appointmentRepository
            .Setup(r => r.GetByIdAsync(appointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Appointment { Id = appointmentId, PatientId = OtherPatientId, Status = "Scheduled" });
        var handler = CreateHandler();
        var request = ValidRequest();
        request.AppointmentId = appointmentId;

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        Assert.False(result.IsNotFound);
    }

    [Fact]
    public async Task BlankComplaintsAndDiagnosis_Succeeds()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.Complaints = null;
        request.Diagnosis = "  ";

        var result = await handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.Null(result.Value!.Complaints);
        Assert.Null(result.Value.Diagnosis);
    }

    [Fact]
    public async Task ValueWithConflictingNotRecordedFlag_NormalizesToNotRecorded()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.TemperatureValue = 101.2m;
        request.TemperatureNotRecorded = true; // conflicting: value present AND flagged not recorded

        var result = await handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.TemperatureNotRecorded);
        Assert.Null(result.Value.TemperatureValue);
    }

    [Fact]
    public async Task ZeroMedications_Succeeds()
    {
        var handler = CreateHandler();
        var request = ValidRequest();

        var result = await handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!.Medications);
    }

    [Fact]
    public async Task OneCompleteMedication_Succeeds()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.Medications = new List<MedicationDto>
        {
            new() { Name = "Paracetamol", Dosage = "500mg", Frequency = "Twice daily", Duration = "5 days", Instructions = "After food" }
        };

        var result = await handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.Medications);
        Assert.Equal("Paracetamol", result.Value.Medications[0].Name);
        Assert.Equal("After food", result.Value.Medications[0].Instructions);
    }

    [Fact]
    public async Task MultipleMedications_SucceedsAndPreservesOrder()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.Medications = new List<MedicationDto>
        {
            new() { Name = "Amoxicillin", Dosage = "250mg", Frequency = "Thrice daily", Duration = "7 days", Instructions = "After food" },
            new() { Name = "Cetirizine", Dosage = "10mg", Frequency = "Once daily", Duration = "3 days", Instructions = "At night" }
        };

        var result = await handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.Medications.Count);
        Assert.Equal("Amoxicillin", result.Value.Medications[0].Name);
        Assert.Equal("Cetirizine", result.Value.Medications[1].Name);
    }

    [Fact]
    public async Task RowWithBlankNameButOtherFieldsPopulated_Fails()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.Medications = new List<MedicationDto>
        {
            new() { Name = "", Dosage = "250mg", Frequency = "Thrice daily", Duration = "7 days", Instructions = "After food" }
        };

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        Assert.False(result.IsNotFound);
    }

    [Fact]
    public async Task TouchedRowMissingSomeFields_Fails()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.Medications = new List<MedicationDto>
        {
            new() { Name = "Ibuprofen", Dosage = null, Frequency = null, Duration = null, Instructions = null }
        };

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task FullyBlankRow_IsSilentlyDropped()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.Medications = new List<MedicationDto>
        {
            new() { Name = "", Dosage = "", Frequency = "", Duration = "", Instructions = "" }
        };

        var result = await handler.HandleAsync(request);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!.Medications);
    }
}
