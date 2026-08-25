namespace PatientManagement.Application.Visits.Dtos;

/// <summary>A single prescribed medicine line item, in submission/display order.</summary>
public class MedicationDto
{
    public string Name { get; set; } = string.Empty;

    public string? Dosage { get; set; }

    public string? Frequency { get; set; }

    public string? Duration { get; set; }

    public string? Instructions { get; set; }
}
