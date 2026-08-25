using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Appointments.Commands;
using PatientManagement.Application.Appointments.Dtos;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Appointments;

public class CreateAppointmentCommandTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ExistingPatientId = Guid.NewGuid();

    public CreateAppointmentCommandTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
        _patientRepository
            .Setup(r => r.GetByIdAsync(ExistingPatientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = ExistingPatientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });
        _appointmentRepository
            .Setup(r => r.GetOverlappingAsync(It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());
    }

    private CreateAppointmentCommandHandler CreateHandler(int slotMinutes = 30) =>
        new(_appointmentRepository.Object, _patientRepository.Object, _dateTimeProvider.Object, Options.Create(new AppointmentOptions { SlotMinutes = slotMinutes }));

    private static CreateAppointmentRequestDto ValidRequest() => new()
    {
        PatientId = ExistingPatientId,
        AppointmentDate = "2026-08-26",
        AppointmentTime = "09:00"
    };

    [Fact]
    public async Task ValidInput_ReturnsSuccessAndPersistsAsScheduled()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(ValidRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(AppointmentStatuses.Scheduled, result.Value!.Status);
        Assert.Equal("Jane Doe", result.Value.PatientName);
        _appointmentRepository.Verify(r => r.AddAsync(It.Is<Appointment>(a => a.PatientId == ExistingPatientId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MissingPatientId_ReturnsFailure()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.PatientId = Guid.Empty;

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        _appointmentRepository.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingDate_ReturnsFailure()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.AppointmentDate = "";

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task MissingTime_ReturnsFailure()
    {
        var handler = CreateHandler();
        var request = ValidRequest();
        request.AppointmentTime = "";

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task UnknownPatientId_ReturnsFailure()
    {
        _patientRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);
        var handler = CreateHandler();
        var request = ValidRequest();
        request.PatientId = Guid.NewGuid();

        var result = await handler.HandleAsync(request);

        Assert.False(result.Succeeded);
        _appointmentRepository.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OverlapDetected_StillSucceedsButFlagsWarning()
    {
        var conflicting = new Appointment { Id = Guid.NewGuid(), PatientId = ExistingPatientId, AppointmentTime = new TimeOnly(9, 15), Status = AppointmentStatuses.Scheduled };
        _appointmentRepository
            .Setup(r => r.GetOverlappingAsync(It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment> { conflicting });

        var handler = CreateHandler();

        var result = await handler.HandleAsync(ValidRequest());

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.HasOverlapWarning);
        Assert.Single(result.Value.ConflictingAppointments);
    }

    [Fact]
    public async Task NoOverlap_ReturnsFalseWarningFlag()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(ValidRequest());

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.HasOverlapWarning);
    }
}
