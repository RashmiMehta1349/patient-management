using PatientManagement.Application.Prescriptions.Dtos;

namespace PatientManagement.Application.Prescriptions.Services;

/// <summary>Renders a composed PrescriptionDocumentDto into a PDF byte stream — implemented in
/// Infrastructure (QuestPDF), kept behind this Application-layer interface so the rendering
/// library is swappable without touching the composition/query logic.</summary>
public interface IPrescriptionPdfGenerator
{
    byte[] Generate(PrescriptionDocumentDto document);
}
