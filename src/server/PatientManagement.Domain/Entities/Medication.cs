using System;

namespace PatientManagement.Domain.Entities;

/// <summary>
/// A single prescribed medicine line item, always scoped to exactly one Visit (no independent
/// lifecycle — see Planning\05_Prescription_and_Medication_Management_Plan.md §4). Name is the one
/// required/identifying field; Dosage/Frequency/Duration/Instructions are free text. SortOrder
/// preserves the doctor's entry order for display/print.
/// </summary>
public class Medication
{
    public long Id { get; set; }

    public long VisitId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Dosage { get; set; }

    public string? Frequency { get; set; }

    public string? Duration { get; set; }

    public string? Instructions { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
