using System;
using System.Collections.Generic;
using PatientManagement.Application.Visits.Dtos;

namespace PatientManagement.Application.Prescriptions.Dtos;

/// <summary>
/// Everything the PDF renderer needs to compose a prescription: patient details, visit date,
/// vitals, diagnosis, and the medication list — a print-time composition of Patient + Visit +
/// Medication + static header/footer content, not a persisted entity (approved plan §4, no
/// separate Prescription table).
/// </summary>
public class PrescriptionDocumentDto
{
    public required string PatientName { get; init; }

    public required string PatientGender { get; init; }

    public required int PatientAge { get; init; }

    public required string PatientPhoneNumber { get; init; }

    public required DateTime VisitDate { get; init; }

    public decimal? TemperatureValue { get; init; }

    public bool TemperatureNotRecorded { get; init; }

    public string? BloodPressureValue { get; init; }

    public bool BloodPressureNotRecorded { get; init; }

    public int? PulseValue { get; init; }

    public bool PulseNotRecorded { get; init; }

    public string? Diagnosis { get; init; }

    public required List<MedicationDto> Medications { get; init; }
}
