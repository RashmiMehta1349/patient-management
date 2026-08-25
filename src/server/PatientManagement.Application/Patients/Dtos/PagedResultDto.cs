using System.Collections.Generic;

namespace PatientManagement.Application.Patients.Dtos;

/// <summary>
/// Generic paged response envelope shared by both the browse-all and search branches of
/// GET /api/patients (Increment 3 revision, §9b.1). Page/PageSize echo back the effective
/// (post-clamp/post-default) values actually applied by the server.
/// </summary>
public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
