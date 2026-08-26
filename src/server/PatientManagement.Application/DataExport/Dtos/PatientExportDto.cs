using System;
using System.Collections.Generic;

namespace PatientManagement.Application.DataExport.Dtos;

/// <summary>
/// Shapes both the CSV and PDF patient export (plan §6). VisitSummaries/AppointmentSummaries are
/// null when includeHistory was false/omitted (profile-only export, the default per plan §5 Open
/// Question 3) and a (possibly empty) list when includeHistory=true was requested — the null/empty
/// distinction lets renderers tell "history not requested" apart from "patient has zero visits/
/// appointments."
/// </summary>
public class PatientExportDto
{
    public required long PatientId { get; init; }

    public required string FullName { get; init; }

    /// <summary>ISO 8601 date string (yyyy-MM-dd), matching PatientDto's convention.</summary>
    public required string DateOfBirth { get; init; }

    public required int Age { get; init; }

    public required string Gender { get; init; }

    public required string PhoneNumber { get; init; }

    public required DateTime RegisteredAt { get; init; }

    public List<VisitSummaryDto>? VisitSummaries { get; init; }

    public List<AppointmentSummaryDto>? AppointmentSummaries { get; init; }
}
