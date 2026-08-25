using System;
using System.Collections.Generic;

namespace PatientManagement.Application.Visits.Dtos;

public class CreateVisitRequestDto
{
    public Guid PatientId { get; set; }

    public Guid? AppointmentId { get; set; }

    public decimal? TemperatureValue { get; set; }

    public bool TemperatureNotRecorded { get; set; }

    public string? BloodPressureValue { get; set; }

    public bool BloodPressureNotRecorded { get; set; }

    public int? PulseValue { get; set; }

    public bool PulseNotRecorded { get; set; }

    public string? Complaints { get; set; }

    public string? Diagnosis { get; set; }

    /// <summary>Module 5 — optional; a fully-blank row is silently dropped server-side, a row with
    /// a blank Name but other fields populated is rejected (§4 ValidateMedications).</summary>
    public List<MedicationDto>? Medications { get; set; }
}
