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

public class UpdateAppointmentCommandTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly long AppointmentId = Random.Shared.NextInt64(1, long.MaxValue);
    private static readonly long PatientId = Random.Shared.NextInt64(1, long.MaxValue);

    public UpdateAppointmentCommandTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
        _patientRepository.Setup(r => r.GetByIdAsync(PatientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = PatientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });
        _appointmentRepository
            .Setup(r => r.GetOverlappingAsync(It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<int>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());
    }

    private UpdateAppointmentCommandHandler CreateHandler() =>
        new(_appointmentRepository.Object, _patientRepository.Object, _dateTimeProvider.Object, Options.Create(new AppointmentOptions { SlotMinutes = 30 }));

    private static Appointment ExistingAppointment() => new()
    {
        Id = AppointmentId,
        PatientId = PatientId,
        AppointmentDate = new DateOnly(2026, 8, 26),
        AppointmentTime = new TimeOnly(9, 0),
        Status = AppointmentStatuses.Scheduled,
        CreatedAt = FixedUtcNow,
        UpdatedAt = FixedUtcNow
    };

    [Fact]
    public async Task ValidEdit_PersistsChanges()
    {
        _appointmentRepository.Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAppointment());

        var result = await CreateHandler().HandleAsync(AppointmentId, new UpdateAppointmentRequestDto { AppointmentDate = "2026-08-27", AppointmentTime = "10:00" });

        Assert.True(result.Succeeded);
        Assert.Equal("2026-08-27", result.Value!.AppointmentDate);
        Assert.Equal("10:00", result.Value.AppointmentTime);
    }

    [Fact]
    public async Task OverlapExcludesSelf_NoWarningWhenOnlyConflictIsSelf()
    {
        _appointmentRepository.Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAppointment());
        _appointmentRepository
            .Setup(r => r.GetOverlappingAsync(It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<int>(), AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());

        var result = await CreateHandler().HandleAsync(AppointmentId, new UpdateAppointmentRequestDto { AppointmentDate = "2026-08-26", AppointmentTime = "09:00" });

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.HasOverlapWarning);
        _appointmentRepository.Verify(r => r.GetOverlappingAsync(It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<int>(), AppointmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidPayload_ReturnsFailure()
    {
        _appointmentRepository.Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingAppointment());

        var result = await CreateHandler().HandleAsync(AppointmentId, new UpdateAppointmentRequestDto { AppointmentDate = "", AppointmentTime = "10:00" });

        Assert.False(result.Succeeded);
        Assert.False(result.IsNotFound);
    }

    [Fact]
    public async Task UnknownId_ReturnsNotFound()
    {
        _appointmentRepository.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Appointment?)null);

        var result = await CreateHandler().HandleAsync(Random.Shared.NextInt64(1, long.MaxValue), new UpdateAppointmentRequestDto { AppointmentDate = "2026-08-27", AppointmentTime = "10:00" });

        Assert.True(result.IsNotFound);
    }
}
