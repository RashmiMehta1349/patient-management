using PatientManagement.Application.DataExport.Dtos;

namespace PatientManagement.Application.DataExport.Services;

/// <summary>New, purpose-built PDF generator for visit export (plan §5) — a sibling to Module 5's
/// IPrescriptionPdfGenerator, not a modification of it, so the tested prescription path is left
/// untouched while export gets the fields it needs (notably Complaints).</summary>
public interface IVisitExportPdfGenerator
{
    byte[] Generate(VisitExportDto document);
}
