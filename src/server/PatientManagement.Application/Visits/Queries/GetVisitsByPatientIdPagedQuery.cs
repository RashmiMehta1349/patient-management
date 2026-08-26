using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Common;
using PatientManagement.Application.Patients;
using PatientManagement.Application.Patients.Dtos;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Application.Visits.Services;

namespace PatientManagement.Application.Visits.Queries;

/// <summary>Paginated variant of GetVisitsByPatientIdQueryHandler, powering the Patient Detail
/// "Consultations" grid's pagination controls. Reuses PatientPaging's page/pageSize
/// defaults/clamping (page 1, size 25, max 100) for consistency with the patients grid.
/// DataExport keeps using the unpaginated GetVisitsByPatientIdQueryHandler — this handler is
/// additive, not a replacement.</summary>
public class GetVisitsByPatientIdPagedQueryHandler
{
    private readonly IVisitRepository _visitRepository;
    private readonly IPatientRepository _patientRepository;

    public GetVisitsByPatientIdPagedQueryHandler(IVisitRepository visitRepository, IPatientRepository patientRepository)
    {
        _visitRepository = visitRepository;
        _patientRepository = patientRepository;
    }

    public async Task<Result<PagedResultDto<VisitDto>>> HandleAsync(
        long patientId,
        int page,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return Result<PagedResultDto<VisitDto>>.Failure("fromDate must not be after toDate.");
        }

        var (normalizedPage, normalizedPageSize) = PatientPaging.Normalize(page, pageSize);

        var normalizedFrom = fromDate?.Date;
        var normalizedTo = toDate?.Date.AddDays(1).AddTicks(-1);

        var (visits, totalCount) = await _visitRepository.GetByPatientIdPagedAsync(
            patientId, normalizedPage, normalizedPageSize, normalizedFrom, normalizedTo, cancellationToken);

        var patientName = string.Empty;
        if (visits.Count > 0)
        {
            var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
            patientName = patient?.FullName ?? string.Empty;
        }

        return Result<PagedResultDto<VisitDto>>.Success(new PagedResultDto<VisitDto>
        {
            Items = visits.Select(v => VisitMapper.ToDto(v, patientName)).ToList(),
            TotalCount = totalCount,
            Page = normalizedPage,
            PageSize = normalizedPageSize
        });
    }
}
