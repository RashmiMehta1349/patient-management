using System;

namespace PatientManagement.Domain.Entities;

/// <summary>
/// A registered patient. The anchor entity for Appointments, Consultations, Prescriptions,
/// History, Search, and Export (all take a FK dependency on this table).
/// </summary>
public class Patient
{
    public long Id { get; set; }

    /// <summary>Required. Indexed for name-based search (Increment 3).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Canonical date-of-birth storage; Age is computed on read (DTO/UI), never persisted,
    /// to avoid drift as time passes.
    /// </summary>
    public DateOnly DateOfBirth { get; set; }

    /// <summary>Fixed picklist: "Male", "Female", or "Other".</summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>Required. Indexed for phone-based search (Increment 3). No uniqueness constraint.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
