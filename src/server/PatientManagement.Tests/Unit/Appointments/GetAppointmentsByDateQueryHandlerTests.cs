using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Appointments.Queries;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Appointments;

public class GetAppointmentsByDateQueryHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 26, 8, 0, 0, DateTimeKind.Utc);

    public GetAppointmentsByDateQueryHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private GetAppointmentsByDateQueryHandler CreateHandler() => new(_appointmentRepository.Object, _patientRepository.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task ReturnsAppointmentsInTimeOrderWithPatientNames()
    {
        var date = new DateOnly(2026, 8, 26);
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        var appointments = new List<Appointment>
        {
            new() { Id = Random.Shared.NextInt64(1, long.MaxValue), PatientId = patientId, AppointmentDate = date, AppointmentTime = new TimeOnly(9, 0), Status = AppointmentStatuses.Scheduled },
            new() { Id = Random.Shared.NextInt64(1, long.MaxValue), PatientId = patientId, AppointmentDate = date, AppointmentTime = new TimeOnly(10, 30), Status = AppointmentStatuses.Scheduled }
        };
        _appointmentRepository.Setup(r => r.GetByDateAsync(date, It.IsAny<CancellationToken>())).ReturnsAsync(appointments);
        _patientRepository.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var result = await CreateHandler().HandleAsync(date);

        Assert.Equal(2, result.Count);
        Assert.Equal("09:00", result[0].AppointmentTime);
        Assert.Equal("Jane Doe", result[0].PatientName);
    }

    [Fact]
    public async Task PastScheduledAppointment_IsAutoTransitionedToNoShow()
    {
        var pastDate = new DateOnly(2026, 8, 20);
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        var appointment = new Appointment { Id = Random.Shared.NextInt64(1, long.MaxValue), PatientId = patientId, AppointmentDate = pastDate, AppointmentTime = new TimeOnly(9, 0), Status = AppointmentStatuses.Scheduled };
        _appointmentRepository.Setup(r => r.GetByDateAsync(pastDate, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment> { appointment });
        _patientRepository.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var result = await CreateHandler().HandleAsync(pastDate);

        Assert.Equal(AppointmentStatuses.NoShow, result[0].Status);
        Assert.Equal(AppointmentStatuses.NoShow, appointment.Status);
        _appointmentRepository.Verify(r => r.UpdateAsync(It.Is<Appointment>(a => a.Status == AppointmentStatuses.NoShow), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TodaysScheduledAppointment_IsNotAutoTransitioned()
    {
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        var today = DateOnly.FromDateTime(FixedUtcNow);
        var appointment = new Appointment { Id = Random.Shared.NextInt64(1, long.MaxValue), PatientId = patientId, AppointmentDate = today, AppointmentTime = new TimeOnly(9, 0), Status = AppointmentStatuses.Scheduled };
        _appointmentRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment> { appointment });
        _patientRepository.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var result = await CreateHandler().HandleAsync(today);

        Assert.Equal(AppointmentStatuses.Scheduled, result[0].Status);
        _appointmentRepository.Verify(r => r.UpdateAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmptyDay_ReturnsEmptyArray()
    {
        var date = new DateOnly(2026, 8, 27);
        _appointmentRepository.Setup(r => r.GetByDateAsync(date, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment>());

        var result = await CreateHandler().HandleAsync(date);

        Assert.Empty(result);
    }
}
