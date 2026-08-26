using System;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Common;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Application.Visits.Services;

namespace PatientManagement.Application.Visits.Commands;

/// <summary>
/// Full edit of a saved consultation (vitals/complaints/diagnosis) — built as part of the initial
/// pass per explicit product decision (saved consultations are editable, not append-only/deferred).
/// PatientId/AppointmentId are not changeable via this endpoint. Reuses VisitValidation so create
/// and edit can never drift apart on the vitals normalization rule.
/// </summary>
public class UpdateVisitCommandHandler
{
    private readonly IVisitRepository _visitRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateVisitCommandHandler(
        IVisitRepository visitRepository,
        IPatientRepository patientRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _visitRepository = visitRepository;
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<VisitDto>> HandleAsync(long id, UpdateVisitRequestDto request, CancellationToken cancellationToken = default)
    {
        var visit = await _visitRepository.GetByIdAsync(id, cancellationToken);
        if (visit is null)
        {
            return Result<VisitDto>.NotFound("Visit not found.");
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

        visit.TemperatureValue = temperatureValue;
        visit.TemperatureNotRecorded = temperatureNotRecorded;
        visit.BloodPressureValue = bloodPressureValue;
        visit.BloodPressureNotRecorded = bloodPressureNotRecorded;
        visit.PulseValue = pulseValue;
        visit.PulseNotRecorded = pulseNotRecorded;
        visit.Complaints = VisitValidation.NormalizeFreeText(request.Complaints);
        visit.Diagnosis = VisitValidation.NormalizeFreeText(request.Diagnosis);
        var now = _dateTimeProvider.UtcNow;
        visit.UpdatedAt = now;

        foreach (var medication in medications)
        {
            medication.VisitId = visit.Id;
            medication.CreatedAt = now;
            medication.UpdatedAt = now;
        }

        // Replace-on-save (§4): stage delete-existing/insert-submitted, then persist together with
        // the visit's own field changes in a single SaveChangesAsync call (UpdateAsync below).
        await _visitRepository.ReplaceMedicationsAsync(visit.Id, medications, cancellationToken);
        visit.Medications = medications;

        await _visitRepository.UpdateAsync(visit, cancellationToken);

        var patient = await _patientRepository.GetByIdAsync(visit.PatientId, cancellationToken);
        var dto = VisitMapper.ToDto(visit, patient?.FullName ?? string.Empty);

        return Result<VisitDto>.Success(dto);
    }
}
