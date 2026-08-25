using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Application.Visits.Services;

namespace PatientManagement.Application.Visits.Queries;

/// <summary>Patient-scoped visit list, most-recent-first — powers the Patient Detail
/// "Consultations" section. Empty array (not 404) when the patient has none.</summary>
public class GetVisitsByPatientIdQueryHandler
{
    private readonly IVisitRepository _visitRepository;
    private readonly IPatientRepository _patientRepository;

    public GetVisitsByPatientIdQueryHandler(IVisitRepository visitRepository, IPatientRepository patientRepository)
    {
        _visitRepository = visitRepository;
        _patientRepository = patientRepository;
    }

    public async Task<IReadOnlyList<VisitDto>> HandleAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var visits = await _visitRepository.GetByPatientIdAsync(patientId, cancellationToken);
        if (visits.Count == 0)
        {
            return new List<VisitDto>();
        }

        var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
        var patientName = patient?.FullName ?? string.Empty;

        return visits.Select(v => VisitMapper.ToDto(v, patientName)).ToList();
    }
}
