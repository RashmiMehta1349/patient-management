using System;
using System.Collections.Generic;
using System.Linq;
using PatientManagement.Application.DataExport.Dtos;
using PatientManagement.Infrastructure.Services;
using UglyToad.PdfPig;
using Xunit;

namespace PatientManagement.Tests.Unit.DataExport;

public class QuestPdfVisitExportGeneratorTests
{
    static QuestPdfVisitExportGeneratorTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private readonly QuestPdfVisitExportGenerator _generator = new();

    private static VisitExportDto FullyPopulated() => new()
    {
        VisitId = Random.Shared.NextInt64(1, long.MaxValue),
        PatientName = "Jane Doe",
        VisitDate = new DateTime(2026, 8, 25),
        TemperatureValue = 98.6m,
        BloodPressureValue = "120/80",
        PulseValue = 72,
        Complaints = "Fever and cough",
        Diagnosis = "Viral infection",
        Medications = new List<MedicationExportDto>
        {
            new() { Name = "Paracetamol", Dosage = "500mg", Frequency = "Twice daily", Duration = "5 days", Instructions = "After food" }
        }
    };

    private static VisitExportDto AllNotRecordedNoMedications() => new()
    {
        VisitId = Random.Shared.NextInt64(1, long.MaxValue),
        PatientName = "John Smith",
        VisitDate = new DateTime(2026, 8, 25),
        TemperatureNotRecorded = true,
        BloodPressureNotRecorded = true,
        PulseNotRecorded = true,
        Medications = new List<MedicationExportDto>()
    };

    [Fact]
    public void Generate_ReturnsNonEmptyValidPdf()
    {
        var bytes = _generator.Generate(FullyPopulated());

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        using var pdf = PdfDocument.Open(bytes);
        Assert.True(pdf.NumberOfPages >= 1);
    }

    [Fact]
    public void Generate_FullyPopulated_ContainsComplaintsAndAllSections()
    {
        var bytes = _generator.Generate(FullyPopulated());

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        Assert.Contains("Jane Doe", text);
        Assert.Contains("Fever and cough", text);
        Assert.Contains("Viral infection", text);
        Assert.Contains("Paracetamol", text);
    }

    [Fact]
    public void Generate_AllVitalsNotRecorded_ShowsNotRecordedState()
    {
        var bytes = _generator.Generate(AllNotRecordedNoMedications());

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        Assert.Contains("Not recorded", text);
        Assert.Contains("No medications prescribed.", text);
    }
}
