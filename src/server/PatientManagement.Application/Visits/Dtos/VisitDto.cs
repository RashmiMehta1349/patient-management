using System;
using System.Collections.Generic;

namespace PatientManagement.Application.Visits.Dtos;

public class VisitDto
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    /// <summary>Hydrated from Patient.FullName for display — not persisted on Visit (no
    /// denormalized PII duplication beyond this read-time join, matching Module 3's precedent).</summary>
    public string PatientName { get; set; } = string.Empty;

    public Guid? AppointmentId { get; set; }

    public DateTime VisitDate { get; set; }

    public decimal? TemperatureValue { get; set; }

    public bool TemperatureNotRecorded { get; set; }

    public string? BloodPressureValue { get; set; }

    public bool BloodPressureNotRecorded { get; set; }

    public int? PulseValue { get; set; }

    public bool PulseNotRecorded { get; set; }

    public string? Complaints { get; set; }

    public string? Diagnosis { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Module 5 — ordered by SortOrder. Empty (not null) when the visit has no
    /// prescribed medicines (AC5 — never a required field).</summary>
    public List<MedicationDto> Medications { get; set; } = new();
}
