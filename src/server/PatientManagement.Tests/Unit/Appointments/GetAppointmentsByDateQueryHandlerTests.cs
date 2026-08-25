using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Appointments.Queries;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Appointments;

public class GetAppointmentsByDateQueryHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();

    private GetAppointmentsByDateQueryHandler CreateHandler() => new(_appointmentRepository.Object, _patientRepository.Object);

    [Fact]
    public async Task ReturnsAppointmentsInTimeOrderWithPatientNames()
    {
        var date = new DateOnly(2026, 8, 26);
        var patientId = Guid.NewGuid();
        var appointments = new List<Appointment>
        {
            new() { Id = Guid.NewGuid(), PatientId = patientId, AppointmentDate = date, AppointmentTime = new TimeOnly(9, 0), Status = AppointmentStatuses.Scheduled },
            new() { Id = Guid.NewGuid(), PatientId = patientId, AppointmentDate = date, AppointmentTime = new TimeOnly(10, 30), Status = AppointmentStatuses.Scheduled }
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
    public async Task EmptyDay_ReturnsEmptyArray()
    {
        var date = new DateOnly(2026, 8, 27);
        _appointmentRepository.Setup(r => r.GetByDateAsync(date, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment>());

        var result = await CreateHandler().HandleAsync(date);

        Assert.Empty(result);
    }
}
