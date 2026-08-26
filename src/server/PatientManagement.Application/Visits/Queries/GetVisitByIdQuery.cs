using System;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Application.Visits.Services;

namespace PatientManagement.Application.Visits.Queries;

/// <summary>Not-found is a normal, expected outcome for a GET-by-id, not an application error —
/// mirrors GetAppointmentByIdQueryHandler's null-return convention (no Result&lt;T&gt; wrapper here).</summary>
public class GetVisitByIdQueryHandler
{
    private readonly IVisitRepository _visitRepository;
    private readonly IPatientRepository _patientRepository;

    public GetVisitByIdQueryHandler(IVisitRepository visitRepository, IPatientRepository patientRepository)
    {
        _visitRepository = visitRepository;
        _patientRepository = patientRepository;
    }

    public async Task<VisitDto?> HandleAsync(long id, CancellationToken cancellationToken = default)
    {
        var visit = await _visitRepository.GetByIdAsync(id, cancellationToken);
        if (visit is null)
        {
            return null;
        }

        var patient = await _patientRepository.GetByIdAsync(visit.PatientId, cancellationToken);
        return VisitMapper.ToDto(visit, patient?.FullName ?? string.Empty);
    }
}
