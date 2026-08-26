using System;

namespace PatientManagement.Application.DataExport.Dtos;

/// <summary>One row inside a patient export's Appointments section — mirrors the fields shown on
/// the Patient Detail "Appointments" tab (date, time, status, notes), read-only summary only.</summary>
public class AppointmentSummaryDto
{
    /// <summary>ISO 8601 date string (yyyy-MM-dd).</summary>
    public required string AppointmentDate { get; init; }

    /// <summary>24-hour "HH:mm" string.</summary>
    public required string AppointmentTime { get; init; }

    public required string Status { get; init; }

    public string? Notes { get; init; }
}
