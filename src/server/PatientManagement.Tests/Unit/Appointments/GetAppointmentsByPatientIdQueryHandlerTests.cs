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

public class GetAppointmentsByPatientIdQueryHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);

    public GetAppointmentsByPatientIdQueryHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private GetAppointmentsByPatientIdQueryHandler CreateHandler() => new(_appointmentRepository.Object, _patientRepository.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task ReturnsOnlyThatPatientsAppointments()
    {
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        var appointments = new List<Appointment>
        {
            new() { Id = Random.Shared.NextInt64(1, long.MaxValue), PatientId = patientId, AppointmentDate = new DateOnly(2026, 8, 26), AppointmentTime = new TimeOnly(9, 0), Status = AppointmentStatuses.Scheduled }
        };
        _appointmentRepository.Setup(r => r.GetByPatientIdAsync(patientId, It.IsAny<CancellationToken>())).ReturnsAsync(appointments);
        _patientRepository.Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });

        var result = await CreateHandler().HandleAsync(patientId);

        Assert.Single(result);
        Assert.Equal("Jane Doe", result[0].PatientName);
    }

    [Fact]
    public async Task NoAppointments_ReturnsEmptyArray()
    {
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        _appointmentRepository.Setup(r => r.GetByPatientIdAsync(patientId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment>());

        var result = await CreateHandler().HandleAsync(patientId);

        Assert.Empty(result);
    }
}
