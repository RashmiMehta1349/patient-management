using PatientManagement.Application.DataExport.Dtos;
using PatientManagement.Application.DataExport.Services;
using PatientManagement.Application.Prescriptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PatientManagement.Infrastructure.Services;

/// <summary>
/// Purpose-built PDF generator for patient export (plan §5, §10 task 10). Renders profile fields
/// always; renders the optional summarized visit-history table only when
/// PatientExportDto.VisitSummaries is non-null (i.e. includeHistory=true was requested), with an
/// explicit "No visits recorded." state when the list is non-null but empty.
/// </summary>
public class QuestPdfPatientExportGenerator : IPatientExportPdfGenerator
{
    public byte[] Generate(PatientExportDto document)
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
                    header.Item().PaddingTop(4).Text("Patient Export").FontSize(10).Italic();
                    header.Item().PaddingTop(8).LineHorizontal(1);
                });

                page.Content().PaddingTop(10).Column(content =>
                {
                    content.Item().Text(text =>
                    {
                        text.Span("Name: ").SemiBold();
                        text.Span(document.FullName);
                    });
                    content.Item().PaddingTop(2).Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Date of Birth: ").SemiBold();
                            text.Span($"{document.DateOfBirth} (age {document.Age})");
                        });
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Gender: ").SemiBold();
                            text.Span(document.Gender);
                        });
                    });
                    content.Item().PaddingTop(2).Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Phone: ").SemiBold();
                            text.Span(document.PhoneNumber);
                        });
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Registered: ").SemiBold();
                            text.Span(document.RegisteredAt.ToString("dd MMM yyyy"));
                        });
                    });

                    if (document.VisitSummaries is not null)
                    {
                        content.Item().PaddingTop(14).Text("Visit History").SemiBold().FontSize(12);

                        if (document.VisitSummaries.Count == 0)
                        {
                            content.Item().PaddingTop(4).Text("No visits recorded.").Italic();
                        }
                        else
                        {
                            foreach (var summary in document.VisitSummaries)
                            {
                                content.Item().PaddingTop(8).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1f);
                                        columns.RelativeColumn(3f);
                                    });

                                    table.Header(headerRow =>
                                    {
                                        headerRow.Cell().Element(HeaderCell).Text("Date");
                                        headerRow.Cell().Element(HeaderCell).Text("Temp");
                                        headerRow.Cell().Element(HeaderCell).Text("BP");
                                        headerRow.Cell().Element(HeaderCell).Text("Pulse");
                                        headerRow.Cell().Element(HeaderCell).Text("Diagnosis");
                                    });

                                    table.Cell().Element(BodyCell).Text(summary.VisitDate.ToString("dd MMM yyyy"));
                                    table.Cell().Element(BodyCell).Text(summary.TemperatureDisplay);
                                    table.Cell().Element(BodyCell).Text(summary.BloodPressureDisplay);
                                    table.Cell().Element(BodyCell).Text(summary.PulseDisplay);
                                    table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(summary.Diagnosis) ? "-" : summary.Diagnosis);

                                    static IContainer HeaderCell(IContainer c) =>
                                        c.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(4).BorderBottom(1);

                                    static IContainer BodyCell(IContainer c) =>
                                        c.PaddingVertical(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                                });

                                if (summary.Medications.Count == 0)
                                {
                                    content.Item().PaddingTop(2).Text("No medications prescribed.").Italic().FontSize(9);
                                }
                                else
                                {
                                    content.Item().PaddingTop(2).Table(medsTable =>
                                    {
                                        medsTable.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(1.5f);
                                            columns.RelativeColumn(1f);
                                            columns.RelativeColumn(1f);
                                            columns.RelativeColumn(1f);
                                            columns.RelativeColumn(2f);
                                        });

                                        medsTable.Header(headerRow =>
                                        {
                                            headerRow.Cell().Element(MedHeaderCell).Text("Name");
                                            headerRow.Cell().Element(MedHeaderCell).Text("Dosage");
                                            headerRow.Cell().Element(MedHeaderCell).Text("Frequency");
                                            headerRow.Cell().Element(MedHeaderCell).Text("Duration");
                                            headerRow.Cell().Element(MedHeaderCell).Text("Instructions");
                                        });

                                        foreach (var medication in summary.Medications)
                                        {
                                            medsTable.Cell().Element(MedBodyCell).Text(medication.Name);
                                            medsTable.Cell().Element(MedBodyCell).Text(medication.Dosage ?? "-");
                                            medsTable.Cell().Element(MedBodyCell).Text(medication.Frequency ?? "-");
                                            medsTable.Cell().Element(MedBodyCell).Text(medication.Duration ?? "-");
                                            medsTable.Cell().Element(MedBodyCell).Text(medication.Instructions ?? "-");
                                        }

                                        static IContainer MedHeaderCell(IContainer c) =>
                                            c.DefaultTextStyle(x => x.SemiBold().FontSize(9)).PaddingVertical(3).BorderBottom(1);

                                        static IContainer MedBodyCell(IContainer c) =>
                                            c.DefaultTextStyle(x => x.FontSize(9)).PaddingVertical(3).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                                    });
                                }
                            }
                        }
                    }

                    if (document.AppointmentSummaries is not null)
                    {
                        content.Item().PaddingTop(14).Text("Appointments").SemiBold().FontSize(12);

                        if (document.AppointmentSummaries.Count == 0)
                        {
                            content.Item().PaddingTop(4).Text("No appointments recorded.").Italic();
                        }
                        else
                        {
                            content.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1f);
                                    columns.RelativeColumn(1f);
                                    columns.RelativeColumn(2.5f);
                                });

                                table.Header(headerRow =>
                                {
                                    headerRow.Cell().Element(AppointmentHeaderCell).Text("Date");
                                    headerRow.Cell().Element(AppointmentHeaderCell).Text("Time");
                                    headerRow.Cell().Element(AppointmentHeaderCell).Text("Status");
                                    headerRow.Cell().Element(AppointmentHeaderCell).Text("Notes");
                                });

                                foreach (var appointment in document.AppointmentSummaries)
                                {
                                    table.Cell().Element(AppointmentBodyCell).Text(appointment.AppointmentDate);
                                    table.Cell().Element(AppointmentBodyCell).Text(appointment.AppointmentTime);
                                    table.Cell().Element(AppointmentBodyCell).Text(appointment.Status);
                                    table.Cell().Element(AppointmentBodyCell).Text(string.IsNullOrWhiteSpace(appointment.Notes) ? "-" : appointment.Notes);
                                }

                                static IContainer AppointmentHeaderCell(IContainer c) =>
                                    c.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(4).BorderBottom(1);

                                static IContainer AppointmentBodyCell(IContainer c) =>
                                    c.PaddingVertical(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                            });
                        }
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
