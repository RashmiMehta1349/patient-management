using System;
using System.Threading;
using System.Threading.Tasks;
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

public class UpdateAppointmentStatusCommandTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly long AppointmentId = Random.Shared.NextInt64(1, long.MaxValue);

    public UpdateAppointmentStatusCommandTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private UpdateAppointmentStatusCommandHandler CreateHandler() =>
        new(_appointmentRepository.Object, _patientRepository.Object, _dateTimeProvider.Object);

    private static Appointment ExistingAppointment() => new()
    {
        Id = AppointmentId,
        PatientId = Random.Shared.NextInt64(1, long.MaxValue),
        AppointmentDate = new DateOnly(2026, 8, 26),
        AppointmentTime = new TimeOnly(9, 0),
        Status = AppointmentStatuses.Scheduled,
        CreatedAt = FixedUtcNow,
        UpdatedAt = FixedUtcNow
    };

    [Theory]
    [InlineData(AppointmentStatuses.Scheduled)]
    [InlineData(AppointmentStatuses.Completed)]
    [InlineData(AppointmentStatuses.Cancelled)]
    [InlineData(AppointmentStatuses.NoShow)]
    public async Task ValidStatus_UpdatesAndReturnsSuccess(string status)
    {
        var appointment = ExistingAppointment();
        _appointmentRepository.Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);

        var result = await CreateHandler().HandleAsync(AppointmentId, new UpdateAppointmentStatusRequestDto { Status = status });

        Assert.True(result.Succeeded);
        Assert.Equal(status, result.Value!.Status);
        _appointmentRepository.Verify(r => r.UpdateAsync(It.Is<Appointment>(a => a.Status == status), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidStatus_ReturnsFailure()
    {
        var appointment = ExistingAppointment();
        _appointmentRepository.Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);

        var result = await CreateHandler().HandleAsync(AppointmentId, new UpdateAppointmentStatusRequestDto { Status = "Bogus" });

        Assert.False(result.Succeeded);
        Assert.False(result.IsNotFound);
        _appointmentRepository.Verify(r => r.UpdateAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnknownId_ReturnsNotFound()
    {
        _appointmentRepository.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Appointment?)null);

        var result = await CreateHandler().HandleAsync(Random.Shared.NextInt64(1, long.MaxValue), new UpdateAppointmentStatusRequestDto { Status = AppointmentStatuses.Completed });

        Assert.True(result.IsNotFound);
    }
}
