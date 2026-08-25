using System.Collections.Generic;

namespace PatientManagement.Application.Visits.Dtos;

/// <summary>Full edit payload (vitals/complaints/diagnosis/medications — PatientId/AppointmentId
/// are not changeable via this endpoint, mirroring UpdateAppointmentRequestDto's precedent).
/// Medications are replaced wholesale on each save (§4 replace-on-save).</summary>
public class UpdateVisitRequestDto
{
    public decimal? TemperatureValue { get; set; }

    public bool TemperatureNotRecorded { get; set; }

    public string? BloodPressureValue { get; set; }

    public bool BloodPressureNotRecorded { get; set; }

    public int? PulseValue { get; set; }

    public bool PulseNotRecorded { get; set; }

    public string? Complaints { get; set; }

    public string? Diagnosis { get; set; }

    public List<MedicationDto>? Medications { get; set; }
}
