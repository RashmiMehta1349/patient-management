using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Appointments.Dtos;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Common;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Appointments.Commands;

/// <summary>
/// Validates and persists a new Appointment. Overlap detection is computed server-side against
/// the same slot-window/exclusion rules as before (AppointmentOptions.SlotMinutes, Cancelled/
/// NoShow excluded) — but now blocks the save: a conflicting slot fails with a 400 rather than
/// saving with a warning (product decision superseding the original BRD R4 "warn, don't block").
/// </summary>
public class CreateAppointmentCommandHandler
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly Application.Auth.Services.IDateTimeProvider _dateTimeProvider;
    private readonly AppointmentOptions _options;

    public CreateAppointmentCommandHandler(
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

    public async Task<Result<AppointmentDto>> HandleAsync(CreateAppointmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var errors = AppointmentValidation.Validate(
            request.PatientId, request.AppointmentDate, request.AppointmentTime, out var date, out var time,
            DateOnly.FromDateTime(_dateTimeProvider.UtcNow));
        if (errors.Count > 0)
        {
            return Result<AppointmentDto>.Failure(string.Join(" ", errors));
        }

        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result<AppointmentDto>.Failure("Patient not found.");
        }

        var overlaps = await _appointmentRepository.GetOverlappingAsync(date, time, _options.SlotMinutes, excludeAppointmentId: null, cancellationToken);
        if (overlaps.Count > 0)
        {
            return Result<AppointmentDto>.Failure(await AppointmentOverlap.BuildErrorMessageAsync(overlaps, _patientRepository, cancellationToken));
        }

        var now = _dateTimeProvider.UtcNow;
        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            AppointmentDate = date,
            AppointmentTime = time,
            Status = AppointmentStatuses.Scheduled,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _appointmentRepository.AddAsync(appointment, cancellationToken);

        var dto = await AppointmentMapper.ToDtoAsync(appointment, patient.FullName, new List<Appointment>(), _patientRepository, cancellationToken);

        return Result<AppointmentDto>.Success(dto);
    }
}
