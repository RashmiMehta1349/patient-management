using System;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Appointments.Dtos;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Common;
using PatientManagement.Application.Patients.Services;

namespace PatientManagement.Application.Appointments.Commands;

/// <summary>
/// Updates only the Status field — a minimal dedicated endpoint so the fast one/two-click status
/// change (R9) doesn't require resending date/time/notes (approved plan §4). No status-transition
/// state machine is enforced beyond "must be one of the four allowed values" (approved plan §4 —
/// a deliberate scope-limiting decision, not an oversight).
/// </summary>
public class UpdateAppointmentStatusCommandHandler
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly Application.Auth.Services.IDateTimeProvider _dateTimeProvider;

    public UpdateAppointmentStatusCommandHandler(
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        Application.Auth.Services.IDateTimeProvider dateTimeProvider)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AppointmentDto>> HandleAsync(long id, UpdateAppointmentStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken);
        if (appointment is null)
        {
            return Result<AppointmentDto>.NotFound("Appointment not found.");
        }

        if (!AppointmentValidation.IsValidStatus(request.Status))
        {
            return Result<AppointmentDto>.Failure("Status must be one of: Scheduled, Completed, Cancelled, NoShow.");
        }

        appointment.Status = request.Status;
        appointment.UpdatedAt = _dateTimeProvider.UtcNow;

        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);

        var patient = await _patientRepository.GetByIdAsync(appointment.PatientId, cancellationToken);
        var dto = AppointmentMapper.ToDtoWithoutOverlapCheck(appointment, patient?.FullName ?? string.Empty);

        return Result<AppointmentDto>.Success(dto);
    }
}
