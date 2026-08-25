using System;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Common;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Application.Visits.Services;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Visits.Commands;

/// <summary>
/// Validates and persists a new Visit (consultation record). PatientId is required and must
/// reference an existing Patient (R6); AppointmentId is optional but, when supplied, must
/// reference an existing Appointment whose PatientId matches this visit's PatientId. Vitals are
/// normalized (never rejected) per VisitValidation's rules (R2).
/// </summary>
public class CreateVisitCommandHandler
{
    private readonly IVisitRepository _visitRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateVisitCommandHandler(
        IVisitRepository visitRepository,
        IPatientRepository patientRepository,
        IAppointmentRepository appointmentRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _visitRepository = visitRepository;
        _patientRepository = patientRepository;
        _appointmentRepository = appointmentRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<VisitDto>> HandleAsync(CreateVisitRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.PatientId == Guid.Empty)
        {
            return Result<VisitDto>.Failure("Patient is required.");
        }

        var errors = VisitValidation.Validate(
            request.TemperatureValue, request.TemperatureNotRecorded,
            request.BloodPressureValue, request.BloodPressureNotRecorded,
            request.PulseValue, request.PulseNotRecorded,
            out var temperatureValue, out var temperatureNotRecorded,
            out var bloodPressureValue, out var bloodPressureNotRecorded,
            out var pulseValue, out var pulseNotRecorded);

        if (errors.Count > 0)
        {
            return Result<VisitDto>.Failure(string.Join(" ", errors));
        }

        var medicationErrors = VisitValidation.ValidateMedications(request.Medications, out var medications);
        if (medicationErrors.Count > 0)
        {
            return Result<VisitDto>.Failure(string.Join(" ", medicationErrors));
        }

        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result<VisitDto>.NotFound("Patient not found.");
        }

        if (request.AppointmentId.HasValue)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId.Value, cancellationToken);
            if (appointment is null)
            {
                return Result<VisitDto>.NotFound("Appointment not found.");
            }

            if (appointment.PatientId != request.PatientId)
            {
                return Result<VisitDto>.Failure("Appointment does not belong to the specified patient.");
            }
        }

        var now = _dateTimeProvider.UtcNow;
        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId,
            VisitDate = now,
            TemperatureValue = temperatureValue,
            TemperatureNotRecorded = temperatureNotRecorded,
            BloodPressureValue = bloodPressureValue,
            BloodPressureNotRecorded = bloodPressureNotRecorded,
            PulseValue = pulseValue,
            PulseNotRecorded = pulseNotRecorded,
            Complaints = VisitValidation.NormalizeFreeText(request.Complaints),
            Diagnosis = VisitValidation.NormalizeFreeText(request.Diagnosis),
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var medication in medications)
        {
            medication.VisitId = visit.Id;
            medication.CreatedAt = now;
            medication.UpdatedAt = now;
        }
        visit.Medications = medications;

        await _visitRepository.AddAsync(visit, cancellationToken);

        var dto = VisitMapper.ToDto(visit, patient.FullName);
        return Result<VisitDto>.Success(dto);
    }
}
