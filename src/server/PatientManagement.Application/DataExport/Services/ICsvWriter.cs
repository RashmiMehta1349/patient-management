using PatientManagement.Application.DataExport.Dtos;

namespace PatientManagement.Application.DataExport.Services;

/// <summary>
/// Hand-rolled RFC 4180-style CSV composition for the two fixed export shapes this module needs
/// (plan §5 Open Question 1 — hand-rolled over a third-party library, given the narrow, fixed-shape
/// output). Implementations must neutralize CSV-injection payloads (leading =, +, -, @) in
/// free-text fields (plan §12 Security Considerations).
/// </summary>
public interface ICsvWriter
{
    string WriteVisitExport(VisitExportDto document);

    string WritePatientExport(PatientExportDto document);
}
