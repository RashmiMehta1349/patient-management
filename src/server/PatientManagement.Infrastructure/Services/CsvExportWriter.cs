using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PatientManagement.Application.DataExport.Dtos;
using PatientManagement.Application.DataExport.Services;

namespace PatientManagement.Infrastructure.Services;

/// <summary>
/// Hand-rolled RFC 4180-style CSV writer (plan §5 Open Question 1, resolved to hand-rolled over a
/// third-party library given the narrow, fixed-shape output needed). Produces:
///  - Visit export: a "Field,Value" profile/vitals/complaints/diagnosis section, then a
///    Medications sub-table (or an explicit "No medications prescribed." line when empty).
///  - Patient export: a "Field,Value" profile section, then — only when VisitSummaries/
///    AppointmentSummaries are non-null — a Visit History section (each visit's vitals/diagnosis
///    row followed by its own full Medications sub-table) and an Appointments sub-table (or an
///    explicit "No visits/appointments recorded." line when a list is empty but requested).
/// Every free-text field (Complaints/Diagnosis/Instructions) is escaped per RFC 4180 (quote
/// wrapping, embedded-quote doubling) and defended against CSV injection (plan §12): a leading
/// =, +, -, or @ is neutralized with a leading apostrophe before quoting, matching the standard
/// mitigation for spreadsheet formula injection.
/// </summary>
public class CsvExportWriter : ICsvWriter
{
    private const string NewLine = "\r\n";

    public string WriteVisitExport(VisitExportDto document)
    {
        var sb = new StringBuilder();

        WriteRow(sb, "Field", "Value");
        WriteRow(sb, "VisitId", document.VisitId.ToString());
        WriteRow(sb, "PatientName", document.PatientName);
        WriteRow(sb, "VisitDate", document.VisitDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        WriteRow(sb, "Temperature", document.TemperatureNotRecorded ? "Not recorded" : document.TemperatureValue?.ToString(CultureInfo.InvariantCulture) ?? "Not recorded");
        WriteRow(sb, "BloodPressure", document.BloodPressureNotRecorded ? "Not recorded" : document.BloodPressureValue ?? "Not recorded");
        WriteRow(sb, "Pulse", document.PulseNotRecorded ? "Not recorded" : document.PulseValue?.ToString(CultureInfo.InvariantCulture) ?? "Not recorded");
        WriteRow(sb, "Complaints", document.Complaints ?? string.Empty);
        WriteRow(sb, "Diagnosis", document.Diagnosis ?? string.Empty);

        sb.Append(NewLine);
        WriteRow(sb, "Medications");
        if (document.Medications.Count == 0)
        {
            WriteRow(sb, "No medications prescribed.");
        }
        else
        {
            WriteRow(sb, "Name", "Dosage", "Frequency", "Duration", "Instructions");
            foreach (var medication in document.Medications)
            {
                WriteRow(sb, medication.Name, medication.Dosage ?? string.Empty, medication.Frequency ?? string.Empty, medication.Duration ?? string.Empty, medication.Instructions ?? string.Empty);
            }
        }

        return sb.ToString();
    }

    public string WritePatientExport(PatientExportDto document)
    {
        var sb = new StringBuilder();

        WriteRow(sb, "Field", "Value");
        WriteRow(sb, "PatientId", document.PatientId.ToString());
        WriteRow(sb, "FullName", document.FullName);
        WriteRow(sb, "DateOfBirth", document.DateOfBirth);
        WriteRow(sb, "Age", document.Age.ToString(CultureInfo.InvariantCulture));
        WriteRow(sb, "Gender", document.Gender);
        WriteRow(sb, "PhoneNumber", document.PhoneNumber);
        WriteRow(sb, "RegisteredAt", document.RegisteredAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

        if (document.VisitSummaries is not null)
        {
            sb.Append(NewLine);
            WriteRow(sb, "Visit History");
            if (document.VisitSummaries.Count == 0)
            {
                WriteRow(sb, "No visits recorded.");
            }
            else
            {
                foreach (var summary in document.VisitSummaries)
                {
                    WriteRow(sb, "VisitDate", "Temperature", "BloodPressure", "Pulse", "Diagnosis");
                    WriteRow(
                        sb,
                        summary.VisitDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                        summary.TemperatureDisplay,
                        summary.BloodPressureDisplay,
                        summary.PulseDisplay,
                        summary.Diagnosis ?? string.Empty);

                    if (summary.Medications.Count == 0)
                    {
                        WriteRow(sb, "No medications prescribed.");
                    }
                    else
                    {
                        WriteRow(sb, "Name", "Dosage", "Frequency", "Duration", "Instructions");
                        foreach (var medication in summary.Medications)
                        {
                            WriteRow(sb, medication.Name, medication.Dosage ?? string.Empty, medication.Frequency ?? string.Empty, medication.Duration ?? string.Empty, medication.Instructions ?? string.Empty);
                        }
                    }

                    sb.Append(NewLine);
                }
            }
        }

        if (document.AppointmentSummaries is not null)
        {
            sb.Append(NewLine);
            WriteRow(sb, "Appointments");
            if (document.AppointmentSummaries.Count == 0)
            {
                WriteRow(sb, "No appointments recorded.");
            }
            else
            {
                WriteRow(sb, "AppointmentDate", "AppointmentTime", "Status", "Notes");
                foreach (var appointment in document.AppointmentSummaries)
                {
                    WriteRow(
                        sb,
                        appointment.AppointmentDate,
                        appointment.AppointmentTime,
                        appointment.Status,
                        appointment.Notes ?? string.Empty);
                }
            }
        }

        return sb.ToString();
    }

    private static void WriteRow(StringBuilder sb, params string[] fields)
    {
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append(EscapeField(fields[i]));
        }
        sb.Append(NewLine);
    }

    /// <summary>RFC 4180 field escaping plus CSV-injection defense (plan §12): a field beginning
    /// with =, +, -, or @ is prefixed with a leading apostrophe so spreadsheet software treats it
    /// as literal text rather than a formula, before the usual comma/quote/newline quoting rules
    /// are applied.</summary>
    public static string EscapeField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        var value = field;
        if (value.Length > 0 && (value[0] == '=' || value[0] == '+' || value[0] == '-' || value[0] == '@'))
        {
            value = "'" + value;
        }

        var needsQuoting = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
        if (!needsQuoting)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
