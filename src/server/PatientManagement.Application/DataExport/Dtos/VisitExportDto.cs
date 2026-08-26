using System;
using System.Collections.Generic;

namespace PatientManagement.Application.DataExport.Dtos;

/// <summary>
/// Shapes both the CSV and PDF visit export (plan §6). Unlike Prescriptions.Dtos.PrescriptionDocumentDto
/// (Module 5), this explicitly includes Complaints — a deliberate difference documented in the plan
/// (§4.2, §5): the prescription PDF omits Complaints by design, but a visit export must not.
/// </summary>
public class VisitExportDto
{
    public required long VisitId { get; init; }

    public required string PatientName { get; init; }

    public required DateTime VisitDate { get; init; }

    public decimal? TemperatureValue { get; init; }

    public bool TemperatureNotRecorded { get; init; }

    public string? BloodPressureValue { get; init; }

    public bool BloodPressureNotRecorded { get; init; }

    public int? PulseValue { get; init; }

    public bool PulseNotRecorded { get; init; }

    public string? Complaints { get; init; }

    public string? Diagnosis { get; init; }

    public required List<MedicationExportDto> Medications { get; init; }
}
