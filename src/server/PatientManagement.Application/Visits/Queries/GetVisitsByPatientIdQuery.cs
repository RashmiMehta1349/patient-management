using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Common;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Application.Visits.Services;

namespace PatientManagement.Application.Visits.Queries;

/// <summary>Patient-scoped visit list, most-recent-first — powers the Patient Detail
/// "Consultations" section. Empty array (not 404) when the patient has none.
/// Module 6 (Patient History): optional fromDate/toDate (inclusive, date-only granularity)
/// narrow the list to a date range; both omitted returns the full, unfiltered history
/// (regression-safe default matching pre-Module-6 behavior).</summary>
public class GetVisitsByPatientIdQueryHandler
{
    private readonly IVisitRepository _visitRepository;
    private readonly IPatientRepository _patientRepository;

    public GetVisitsByPatientIdQueryHandler(IVisitRepository visitRepository, IPatientRepository patientRepository)
    {
        _visitRepository = visitRepository;
        _patientRepository = patientRepository;
    }

    public async Task<Result<IReadOnlyList<VisitDto>>> HandleAsync(
        Guid patientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return Result<IReadOnlyList<VisitDto>>.Failure("fromDate must not be after toDate.");
        }

        // VisitDate is a full timestamp (recorded at consultation time), not date-only, so the
        // range predicate normalizes to whole-day boundaries: fromDate's start-of-day through
        // toDate's end-of-day, keeping the filter's semantics "by date" (R6) rather than by
        // exact time-of-day, which the doctor never sees or controls.
        var normalizedFrom = fromDate?.Date;
        var normalizedTo = toDate?.Date.AddDays(1).AddTicks(-1);

        var visits = await _visitRepository.GetByPatientIdAsync(patientId, normalizedFrom, normalizedTo, cancellationToken);
        if (visits.Count == 0)
        {
            return Result<IReadOnlyList<VisitDto>>.Success(new List<VisitDto>());
        }

        var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
        var patientName = patient?.FullName ?? string.Empty;

        IReadOnlyList<VisitDto> dtos = visits.Select(v => VisitMapper.ToDto(v, patientName)).ToList();
        return Result<IReadOnlyList<VisitDto>>.Success(dtos);
    }
}
