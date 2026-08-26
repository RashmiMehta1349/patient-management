using System;
using System.Collections.Generic;
using System.Linq;
using PatientManagement.Application.DataExport.Dtos;
using PatientManagement.Infrastructure.Services;
using UglyToad.PdfPig;
using Xunit;

namespace PatientManagement.Tests.Unit.DataExport;

public class QuestPdfPatientExportGeneratorTests
{
    static QuestPdfPatientExportGeneratorTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private readonly QuestPdfPatientExportGenerator _generator = new();

    private static PatientExportDto ProfileOnly() => new()
    {
        PatientId = Random.Shared.NextInt64(1, long.MaxValue),
        FullName = "Jane Doe",
        DateOfBirth = "1990-01-01",
        Age = 36,
        Gender = "Female",
        PhoneNumber = "555-123-4567",
        RegisteredAt = new DateTime(2024, 1, 1),
        VisitSummaries = null
    };

    private static PatientExportDto WithEmptyHistory() => new()
    {
        PatientId = Random.Shared.NextInt64(1, long.MaxValue),
        FullName = "Zero Visits",
        DateOfBirth = "1990-01-01",
        Age = 36,
        Gender = "Male",
        PhoneNumber = "555-000-0000",
        RegisteredAt = new DateTime(2024, 1, 1),
        VisitSummaries = new List<VisitSummaryDto>()
    };

    private static PatientExportDto WithHistory() => new()
    {
        PatientId = Random.Shared.NextInt64(1, long.MaxValue),
        FullName = "Has History",
        DateOfBirth = "1990-01-01",
        Age = 36,
        Gender = "Male",
        PhoneNumber = "555-111-1111",
        RegisteredAt = new DateTime(2024, 1, 1),
        VisitSummaries = new List<VisitSummaryDto>
        {
            new()
            {
                VisitDate = new DateTime(2026, 8, 20),
                TemperatureDisplay = "98.6",
                BloodPressureDisplay = "120/80",
                PulseDisplay = "72",
                Diagnosis = "Flu",
                Medications = new List<MedicationExportDto>
                {
                    new() { Name = "Paracetamol", Dosage = "500mg", Frequency = "Twice daily", Duration = "5 days", Instructions = "After food" }
                }
            }
        }
    };

    [Fact]
    public void Generate_ReturnsNonEmptyValidPdf()
    {
        var bytes = _generator.Generate(ProfileOnly());

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        using var pdf = PdfDocument.Open(bytes);
        Assert.True(pdf.NumberOfPages >= 1);
    }

    [Fact]
    public void Generate_ProfileOnly_OmitsHistorySection()
    {
        var bytes = _generator.Generate(ProfileOnly());

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        Assert.Contains("Jane Doe", text);
        Assert.DoesNotContain("Visit History", text);
    }

    [Fact]
    public void Generate_WithEmptyHistory_ShowsNoVisitsRecordedState()
    {
        var bytes = _generator.Generate(WithEmptyHistory());

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        Assert.Contains("Visit History", text);
        Assert.Contains("No visits recorded.", text);
    }

    [Fact]
    public void Generate_WithHistory_ContainsSummaryRow()
    {
        var bytes = _generator.Generate(WithHistory());

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        Assert.Contains("Has History", text);
        Assert.Contains("Flu", text);
    }
}
