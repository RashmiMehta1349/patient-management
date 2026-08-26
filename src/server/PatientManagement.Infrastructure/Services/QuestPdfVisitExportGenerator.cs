using PatientManagement.Application.DataExport.Dtos;
using PatientManagement.Application.DataExport.Services;
using PatientManagement.Application.Prescriptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PatientManagement.Infrastructure.Services;

/// <summary>
/// Purpose-built PDF generator for visit export (plan §5, §10 task 4) — a sibling to Module 5's
/// QuestPdfPrescriptionGenerator, not a modification of it, so that tested prescription-print path
/// is left untouched. Reuses PrescriptionDocumentConstants for the fixed clinic header/footer (the
/// BRD does not call for a different "brand" for exports vs. prescriptions) and
/// QuestPdfPrescriptionGenerator's structural conventions (A4, 2cm margins, table styling), but
/// renders all VisitExportDto fields including Complaints — the deliberate difference from the
/// prescription PDF, which omits Complaints by design.
/// </summary>
public class QuestPdfVisitExportGenerator : IVisitExportPdfGenerator
{
    public byte[] Generate(VisitExportDto document)
    {
        var pdfDocument = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(header =>
                {
                    header.Item().Text(PrescriptionDocumentConstants.ClinicName).FontSize(18).Bold();
                    header.Item().Text(PrescriptionDocumentConstants.DoctorName).FontSize(12);
                    header.Item().Text(PrescriptionDocumentConstants.ClinicAddressLine).FontSize(9);
                    header.Item().PaddingTop(4).Text("Visit Export").FontSize(10).Italic();
                    header.Item().PaddingTop(8).LineHorizontal(1);
                });

                page.Content().PaddingTop(10).Column(content =>
                {
                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Patient: ").SemiBold();
                            text.Span(document.PatientName);
                        });
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Date: ").SemiBold();
                            text.Span(document.VisitDate.ToString("dd MMM yyyy"));
                        });
                    });

                    content.Item().PaddingTop(12).Text("Vitals").SemiBold().FontSize(12);
                    content.Item().Text(
                        $"Temp: {(document.TemperatureNotRecorded ? "Not recorded" : document.TemperatureValue?.ToString())}   " +
                        $"BP: {(document.BloodPressureNotRecorded ? "Not recorded" : document.BloodPressureValue)}   " +
                        $"Pulse: {(document.PulseNotRecorded ? "Not recorded" : document.PulseValue?.ToString())}");

                    content.Item().PaddingTop(12).Text("Complaints").SemiBold().FontSize(12);
                    content.Item().Text(string.IsNullOrWhiteSpace(document.Complaints) ? "—" : document.Complaints);

                    content.Item().PaddingTop(12).Text("Diagnosis").SemiBold().FontSize(12);
                    content.Item().Text(string.IsNullOrWhiteSpace(document.Diagnosis) ? "—" : document.Diagnosis);

                    content.Item().PaddingTop(12).Text("Medications").SemiBold().FontSize(12);

                    if (document.Medications.Count == 0)
                    {
                        content.Item().PaddingTop(4).Text("No medications prescribed.").Italic();
                    }
                    else
                    {
                        content.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2.5f);
                            });

                            table.Header(headerRow =>
                            {
                                headerRow.Cell().Element(HeaderCell).Text("Name");
                                headerRow.Cell().Element(HeaderCell).Text("Dosage");
                                headerRow.Cell().Element(HeaderCell).Text("Frequency");
                                headerRow.Cell().Element(HeaderCell).Text("Duration");
                                headerRow.Cell().Element(HeaderCell).Text("Instructions");
                            });

                            foreach (var medication in document.Medications)
                            {
                                table.Cell().Element(BodyCell).Text(medication.Name);
                                table.Cell().Element(BodyCell).Text(medication.Dosage ?? "-");
                                table.Cell().Element(BodyCell).Text(medication.Frequency ?? "-");
                                table.Cell().Element(BodyCell).Text(medication.Duration ?? "-");
                                table.Cell().Element(BodyCell).Text(medication.Instructions ?? "-");
                            }

                            static IContainer HeaderCell(IContainer c) =>
                                c.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(4).BorderBottom(1);

                            static IContainer BodyCell(IContainer c) =>
                                c.PaddingVertical(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                        });
                    }
                });

                page.Footer().PaddingTop(10).Column(footer =>
                {
                    footer.Item().LineHorizontal(1);
                    footer.Item().PaddingTop(6).Text(PrescriptionDocumentConstants.FooterNote).FontSize(8).Italic();
                });
            });
        });

        return pdfDocument.GeneratePdf();
    }
}
