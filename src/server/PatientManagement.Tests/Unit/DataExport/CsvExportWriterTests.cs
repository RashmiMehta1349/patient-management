using System;
using System.Collections.Generic;
using PatientManagement.Application.DataExport.Dtos;
using PatientManagement.Infrastructure.Services;
using Xunit;

namespace PatientManagement.Tests.Unit.DataExport;

/// <summary>Plan §13 — verifies RFC 4180 escaping and the CSV-injection defense (§12) rather than
/// just "produces non-empty output."</summary>
public class CsvExportWriterTests
{
    private readonly CsvExportWriter _writer = new();

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("has,comma", "\"has,comma\"")]
    [InlineData("has\"quote", "\"has\"\"quote\"")]
    [InlineData("has\nnewline", "\"has\nnewline\"")]
    public void EscapeField_QuotesAndEscapesAsExpected(string input, string expected)
    {
        Assert.Equal(expected, CsvExportWriter.EscapeField(input));
    }

    [Theory]
    [InlineData("=SUM(A1:A2)", "'=SUM(A1:A2)")]
    [InlineData("+1234", "'+1234")]
    [InlineData("-danger", "'-danger")]
    [InlineData("@cmd", "'@cmd")]
    public void EscapeField_NeutralizesLeadingFormulaCharacters(string input, string expectedPrefixed)
    {
        Assert.Equal(expectedPrefixed, CsvExportWriter.EscapeField(input));
    }

    [Fact]
    public void WriteVisitExport_ProducesHeaderRowAndEscapesFreeTextFields()
    {
        var document = new VisitExportDto
        {
            VisitId = Random.Shared.NextInt64(1, long.MaxValue),
            PatientName = "Jane Doe",
            VisitDate = new DateTime(2026, 8, 25),
            TemperatureValue = 98.6m,
            TemperatureNotRecorded = false,
            BloodPressureValue = "120/80",
            BloodPressureNotRecorded = false,
            PulseValue = 72,
            PulseNotRecorded = false,
            Complaints = "Cough, fever\nand chills",
            Diagnosis = "=EVIL()",
            Medications = new List<MedicationExportDto>()
        };

        var csv = _writer.WriteVisitExport(document);

        Assert.Contains("Field,Value", csv);
        Assert.Contains("Jane Doe", csv);
        Assert.Contains("\"Cough, fever\nand chills\"", csv);
        Assert.Contains("'=EVIL()", csv);
        Assert.Contains("No medications prescribed.", csv);
    }

    [Fact]
    public void WriteVisitExport_WithMedications_IncludesMedicationTable()
    {
        var document = new VisitExportDto
        {
            VisitId = Random.Shared.NextInt64(1, long.MaxValue),
            PatientName = "Jane Doe",
            VisitDate = new DateTime(2026, 8, 25),
            TemperatureNotRecorded = true,
            BloodPressureNotRecorded = true,
            PulseNotRecorded = true,
            Medications = new List<MedicationExportDto>
            {
                new() { Name = "Paracetamol", Dosage = "500mg", Frequency = "Twice daily", Duration = "5 days", Instructions = "After food" }
            }
        };

        var csv = _writer.WriteVisitExport(document);

        Assert.Contains("Name,Dosage,Frequency,Duration,Instructions", csv);
        Assert.Contains("Paracetamol,500mg,Twice daily,5 days,After food", csv);
    }

    [Fact]
    public void WritePatientExport_WithoutHistory_OmitsHistorySection()
    {
        var document = new PatientExportDto
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

        var csv = _writer.WritePatientExport(document);

        Assert.Contains("Jane Doe", csv);
        Assert.DoesNotContain("Visit History", csv);
    }

    [Fact]
    public void WritePatientExport_WithEmptyHistory_ShowsNoVisitsRecordedState()
    {
        var document = new PatientExportDto
        {
            PatientId = Random.Shared.NextInt64(1, long.MaxValue),
            FullName = "Jane Doe",
            DateOfBirth = "1990-01-01",
            Age = 36,
            Gender = "Female",
            PhoneNumber = "555-123-4567",
            RegisteredAt = new DateTime(2024, 1, 1),
            VisitSummaries = new List<VisitSummaryDto>()
        };

        var csv = _writer.WritePatientExport(document);

        Assert.Contains("Visit History", csv);
        Assert.Contains("No visits recorded.", csv);
    }

    [Fact]
    public void WritePatientExport_WithHistory_IncludesSummaryRows()
    {
        var document = new PatientExportDto
        {
            PatientId = Random.Shared.NextInt64(1, long.MaxValue),
            FullName = "Jane Doe",
            DateOfBirth = "1990-01-01",
            Age = 36,
            Gender = "Female",
            PhoneNumber = "555-123-4567",
            RegisteredAt = new DateTime(2024, 1, 1),
            VisitSummaries = new List<VisitSummaryDto>
            {
                new()
                {
                    VisitDate = new DateTime(2026, 8, 20),
                    TemperatureDisplay = "Not recorded",
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

        var csv = _writer.WritePatientExport(document);

        Assert.Contains("VisitDate,Temperature,BloodPressure,Pulse,Diagnosis", csv);
        Assert.Contains("Not recorded,120/80,72,Flu", csv);
        Assert.Contains("Name,Dosage,Frequency,Duration,Instructions", csv);
        Assert.Contains("Paracetamol,500mg,Twice daily,5 days,After food", csv);
    }

    [Fact]
    public void WritePatientExport_VisitWithNoMedications_ShowsNoMedicationsPrescribedState()
    {
        var document = new PatientExportDto
        {
            PatientId = Random.Shared.NextInt64(1, long.MaxValue),
            FullName = "Jane Doe",
            DateOfBirth = "1990-01-01",
            Age = 36,
            Gender = "Female",
            PhoneNumber = "555-123-4567",
            RegisteredAt = new DateTime(2024, 1, 1),
            VisitSummaries = new List<VisitSummaryDto>
            {
                new()
                {
                    VisitDate = new DateTime(2026, 8, 20),
                    TemperatureDisplay = "Not recorded",
                    BloodPressureDisplay = "120/80",
                    PulseDisplay = "72",
                    Diagnosis = "Flu",
                    Medications = new List<MedicationExportDto>()
                }
            }
        };

        var csv = _writer.WritePatientExport(document);

        Assert.Contains("No medications prescribed.", csv);
    }
}
