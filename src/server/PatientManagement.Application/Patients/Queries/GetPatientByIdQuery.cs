using System;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Commands;
using PatientManagement.Application.Patients.Dtos;
using PatientManagement.Application.Patients.Services;

namespace PatientManagement.Application.Patients.Queries;

/// <summary>
/// Not-found is a normal, expected outcome for a GET-by-id, not an application error — mirrors
/// the existing AuthController.Me null-check precedent (no Result&lt;T&gt; wrapper here).
/// </summary>
public class GetPatientByIdQueryHandler
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetPatientByIdQueryHandler(IPatientRepository patientRepository, IDateTimeProvider dateTimeProvider)
    {
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PatientDto?> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await _patientRepository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return null;
        }

        return CreatePatientCommandHandler.ToDto(patient, _dateTimeProvider.UtcNow);
    }
}
