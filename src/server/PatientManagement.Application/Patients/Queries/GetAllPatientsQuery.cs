using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Commands;
using PatientManagement.Application.Patients.Dtos;
using PatientManagement.Application.Patients.Services;

namespace PatientManagement.Application.Patients.Queries;

/// <summary>
/// Browse-all: one page of all patients ordered by FullName ascending (Increment 3 revision,
/// §9b.1). Not a Result&lt;T&gt; wrapper — there is no failure mode here beyond an empty page,
/// which is a normal outcome, not an application error.
/// </summary>
public class GetAllPatientsQueryHandler
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAllPatientsQueryHandler(IPatientRepository patientRepository, IDateTimeProvider dateTimeProvider)
    {
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResultDto<PatientDto>> HandleAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = PatientPaging.Normalize(page, pageSize);

        var (items, totalCount) = await _patientRepository.GetAllAsync(normalizedPage, normalizedPageSize, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        return new PagedResultDto<PatientDto>
        {
            Items = items.Select(p => CreatePatientCommandHandler.ToDto(p, now)).ToList(),
            TotalCount = totalCount,
            Page = normalizedPage,
            PageSize = normalizedPageSize
        };
    }
}
