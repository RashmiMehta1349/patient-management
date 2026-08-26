namespace PatientManagement.Application.DataExport.Dtos;

/// <summary>One prescribed medicine line item inside a visit export (plan §6) — a separate type
/// from Visits.Dtos.MedicationDto so the export shape can evolve independently of the
/// consultation-form shape it happens to mirror today.</summary>
public class MedicationExportDto
{
    public string Name { get; set; } = string.Empty;

    public string? Dosage { get; set; }

    public string? Frequency { get; set; }

    public string? Duration { get; set; }

    public string? Instructions { get; set; }
}
