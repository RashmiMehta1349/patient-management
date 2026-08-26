using System;
using System.Collections.Generic;

namespace PatientManagement.Application.DataExport.Dtos;

/// <summary>One row inside a patient export's optional summarized visit-history section (plan §6) —
/// vitals pre-resolved to a value-or-"Not recorded" display string, diagnosis, and the full
/// per-medication detail (name/dosage/frequency/duration/instructions) prescribed at that visit.</summary>
public class VisitSummaryDto
{
    public required DateTime VisitDate { get; init; }

    public required string TemperatureDisplay { get; init; }

    public required string BloodPressureDisplay { get; init; }

    public required string PulseDisplay { get; init; }

    public string? Diagnosis { get; init; }

    public required List<MedicationExportDto> Medications { get; init; }
}
