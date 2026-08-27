using System;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Appointments.Dtos;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Services;

namespace PatientManagement.Application.Appointments.Queries;

/// <summary>
/// Not-found is a normal, expected outcome for a GET-by-id, not an application error — mirrors
/// GetPatientByIdQueryHandler's null-return convention (no Result&lt;T&gt; wrapper here).
/// </summary>
public class GetAppointmentByIdQueryHandler
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAppointmentByIdQueryHandler(IAppointmentRepository appointmentRepository, IPatientRepository patientRepository, IDateTimeProvider dateTimeProvider)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AppointmentDto?> HandleAsync(long id, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken);
        if (appointment is null)
        {
            return null;
        }

        var now = _dateTimeProvider.UtcNow;
        if (AppointmentAutoStatus.ShouldAutoNoShow(appointment, DateOnly.FromDateTime(now)))
        {
            appointment.Status = AppointmentStatuses.NoShow;
            appointment.UpdatedAt = now;
            await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
        }

        var patient = await _patientRepository.GetByIdAsync(appointment.PatientId, cancellationToken);
        return AppointmentMapper.ToDtoWithoutOverlapCheck(appointment, patient?.FullName ?? string.Empty);
    }
}
