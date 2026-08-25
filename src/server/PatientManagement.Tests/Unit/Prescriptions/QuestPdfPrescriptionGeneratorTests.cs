using System;
using System.Collections.Generic;
using PatientManagement.Application.Prescriptions;
using PatientManagement.Application.Prescriptions.Dtos;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Infrastructure.Services;
using UglyToad.PdfPig;
using Xunit;

namespace PatientManagement.Tests.Unit.Prescriptions;

/// <summary>
/// Verifies the generated PDF is a valid, non-empty document containing the expected sections
/// (header/patient/vitals/diagnosis/medications/footer) — not just that bytes were returned.
/// </summary>
public class QuestPdfPrescriptionGeneratorTests
{
    static QuestPdfPrescriptionGeneratorTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private readonly QuestPdfPrescriptionGenerator _generator = new();

    private static PrescriptionDocumentDto DocumentWithMedications() => new()
    {
        PatientName = "Jane Doe",
        PatientGender = "Female",
        PatientAge = 34,
        PatientPhoneNumber = "555-123-4567",
        VisitDate = new DateTime(2026, 8, 25),
        TemperatureValue = 98.6m,
        TemperatureNotRecorded = false,
        BloodPressureValue = "120/80",
        BloodPressureNotRecorded = false,
        PulseValue = 72,
        PulseNotRecorded = false,
        Diagnosis = "Viral infection",
        Medications = new List<MedicationDto>
        {
            new() { Name = "Paracetamol", Dosage = "500mg", Frequency = "Twice daily", Duration = "5 days", Instructions = "After food" }
        }
    };

    private static PrescriptionDocumentDto DocumentWithNoMedications() => new()
    {
        PatientName = "John Smith",
        PatientGender = "Male",
        PatientAge = 45,
        PatientPhoneNumber = "555-987-6543",
        VisitDate = new DateTime(2026, 8, 25),
        TemperatureNotRecorded = true,
        BloodPressureNotRecorded = true,
        PulseNotRecorded = true,
        Diagnosis = null,
        Medications = new List<MedicationDto>()
    };

    [Fact]
    public void Generate_ReturnsNonEmptyValidPdf()
    {
        var bytes = _generator.Generate(DocumentWithMedications());

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        using var pdf = PdfDocument.Open(bytes);
        Assert.True(pdf.NumberOfPages >= 1);
    }

    [Fact]
    public void Generate_WithMedications_ContainsExpectedSections()
    {
        var bytes = _generator.Generate(DocumentWithMedications());

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages()).Length > 0
            ? string.Join(" ", System.Linq.Enumerable.Select(pdf.GetPages(), p => p.Text))
            : string.Empty;

        Assert.Contains(PrescriptionDocumentConstants.ClinicName, text);
        Assert.Contains(PrescriptionDocumentConstants.DoctorName, text);
        Assert.Contains("Jane Doe", text);
        Assert.Contains("Viral infection", text);
        Assert.Contains("Paracetamol", text);
        Assert.Contains("500mg", text);
        Assert.Contains(PrescriptionDocumentConstants.FooterNote, text);
    }

    [Fact]
    public void Generate_WithNoMedications_ShowsEmptyStateNotBrokenTable()
    {
        var bytes = _generator.Generate(DocumentWithNoMedications());

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", System.Linq.Enumerable.Select(pdf.GetPages(), p => p.Text));

        Assert.Contains("No medications prescribed", text);
        Assert.Contains("John Smith", text);
    }
}
