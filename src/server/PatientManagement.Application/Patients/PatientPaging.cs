namespace PatientManagement.Application.Patients;

/// <summary>
/// Shared pagination defaults/bounds applied identically by both the browse-all and search
/// branches of GET /api/patients (Increment 3 revision, §9b.1). page defaults to 1 (1-based);
/// pageSize defaults to 25 and is clamped to a max of 100. Out-of-range values (page &lt; 1,
/// pageSize &lt; 1) fall back to the default rather than failing with a 400 — a malformed page
/// param shouldn't hard-fail a grid render.
/// </summary>
public static class PatientPaging
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? DefaultPage : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : pageSize;
        if (normalizedPageSize > MaxPageSize)
        {
            normalizedPageSize = MaxPageSize;
        }

        return (normalizedPage, normalizedPageSize);
    }
}
