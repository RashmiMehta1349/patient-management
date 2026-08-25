using System;
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
/// Validates and persists a new Appointment. Overlap detection is computed server-side and
/// annotated on the success response as a non-blocking warning (approved plan §4) — the save
/// always succeeds when validation passes and the patient exists.
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
        var errors = AppointmentValidation.Validate(request.PatientId, request.AppointmentDate, request.AppointmentTime, out var date, out var time);
        if (errors.Count > 0)
        {
            return Result<AppointmentDto>.Failure(string.Join(" ", errors));
        }

        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result<AppointmentDto>.Failure("Patient not found.");
        }

        var now = _dateTimeProvider.UtcNow;
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            AppointmentDate = date,
            AppointmentTime = time,
            Status = AppointmentStatuses.Scheduled,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _appointmentRepository.AddAsync(appointment, cancellationToken);

        var overlaps = await _appointmentRepository.GetOverlappingAsync(date, time, _options.SlotMinutes, appointment.Id, cancellationToken);

        var dto = await AppointmentMapper.ToDtoAsync(appointment, patient.FullName, overlaps, _patientRepository, cancellationToken);

        return Result<AppointmentDto>.Success(dto);
    }
}
