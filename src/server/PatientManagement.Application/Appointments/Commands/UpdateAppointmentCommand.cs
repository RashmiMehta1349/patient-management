using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Appointments.Dtos;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Common;
using PatientManagement.Application.Patients.Services;

namespace PatientManagement.Application.Appointments.Commands;

/// <summary>
/// Full edit (date/time/notes) — reschedule flow (Increment 3, approved plan §4/§9 task 21).
/// Re-runs overlap detection excluding the appointment's own id, so editing to the same slot it
/// already occupies never warns against itself.
/// </summary>
public class UpdateAppointmentCommandHandler
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly Application.Auth.Services.IDateTimeProvider _dateTimeProvider;
    private readonly AppointmentOptions _options;

    public UpdateAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        Application.Auth.Services.IDateTimeProvider dateTimeProvider,
        IOptions<AppointmentOptions> options)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
    }

    public async Task<Result<AppointmentDto>> HandleAsync(Guid id, UpdateAppointmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken);
        if (appointment is null)
        {
            return Result<AppointmentDto>.NotFound("Appointment not found.");
        }

        var errors = AppointmentValidation.Validate(appointment.PatientId, request.AppointmentDate, request.AppointmentTime, out var date, out var time);
        if (errors.Count > 0)
        {
            return Result<AppointmentDto>.Failure(string.Join(" ", errors));
        }

        appointment.AppointmentDate = date;
        appointment.AppointmentTime = time;
        appointment.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        appointment.UpdatedAt = _dateTimeProvider.UtcNow;

        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);

        var overlaps = await _appointmentRepository.GetOverlappingAsync(date, time, _options.SlotMinutes, appointment.Id, cancellationToken);

        var patient = await _patientRepository.GetByIdAsync(appointment.PatientId, cancellationToken);
        var dto = await AppointmentMapper.ToDtoAsync(appointment, patient?.FullName ?? string.Empty, overlaps, _patientRepository, cancellationToken);

        return Result<AppointmentDto>.Success(dto);
    }
}
