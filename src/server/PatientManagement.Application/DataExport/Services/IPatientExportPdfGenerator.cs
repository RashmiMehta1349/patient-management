using PatientManagement.Application.DataExport.Dtos;

namespace PatientManagement.Application.DataExport.Services;

/// <summary>New, purpose-built PDF generator for patient export (plan §5) — renders profile fields
/// always, and the optional summarized visit-history table only when PatientExportDto.VisitSummaries
/// is non-null.</summary>
public interface IPatientExportPdfGenerator
{
    byte[] Generate(PatientExportDto document);
}
