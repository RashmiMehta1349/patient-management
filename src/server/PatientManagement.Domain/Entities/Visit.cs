using System;
using System.Collections.Generic;

namespace PatientManagement.Domain.Entities;

/// <summary>
/// A recorded consultation (vitals + complaints + diagnosis) tied to an existing Patient (R6) and,
/// optionally, an existing Appointment. Each vital is stored as a nullable value column plus a
/// required boolean "NotRecorded" flag rather than inferring "not recorded" from a null value alone
/// (approved plan §4) — this keeps "doctor forgot" distinct from "doctor explicitly skipped it".
/// </summary>
public class Visit
{
    public long Id { get; set; }

    public long PatientId { get; set; }

    public long? AppointmentId { get; set; }

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

    /// <summary>Module 5 — the visit's prescribed medicines, ordered by SortOrder. A visit's
    /// medication set has no independent lifecycle: it is replaced wholesale on each visit save.</summary>
    public List<Medication> Medications { get; set; } = new();
}
