using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Commands;
using PatientManagement.Application.Patients.Dtos;
using PatientManagement.Application.Patients.Services;

namespace PatientManagement.Application.Patients.Queries;

/// <summary>
/// Case-insensitive partial match against FullName and PhoneNumber, paginated, ordered by
/// FullName ascending (Increment 3, §9b.1) — reworked to share the same paged envelope shape
/// as <see cref="GetAllPatientsQueryHandler"/>.
/// </summary>
public class SearchPatientsQueryHandler
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SearchPatientsQueryHandler(IPatientRepository patientRepository, IDateTimeProvider dateTimeProvider)
    {
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResultDto<PatientDto>> HandleAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = PatientPaging.Normalize(page, pageSize);

        var (items, totalCount) = await _patientRepository.SearchAsync(query, normalizedPage, normalizedPageSize, cancellationToken);

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
