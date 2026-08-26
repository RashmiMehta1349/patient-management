using System;
using System.Collections.Generic;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Visits;

/// <summary>
/// Shared field-validation/normalization logic for Create/Update visit requests (mirrors
/// AppointmentValidation.cs's precedent). Vitals are the one field-set in the app where the server
/// is deliberately permissive: a payload with both a value and NotRecorded=true is normalized
/// (NotRecorded wins) rather than rejected (R2 — never a hard save-blocker). An untouched field
/// (no value, NotRecorded not set) is also normalized to NotRecorded=true, matching the client's
/// own "untouched defaults to Not recorded" submit behavior, so the server never depends solely on
/// client-side normalization having happened correctly.
/// </summary>
public static class VisitValidation
{
    public static List<string> Validate(
        decimal? temperatureValue,
        bool temperatureNotRecorded,
        string? bloodPressureValue,
        bool bloodPressureNotRecorded,
        int? pulseValue,
        bool pulseNotRecorded,
        out decimal? normalizedTemperatureValue,
        out bool normalizedTemperatureNotRecorded,
        out string? normalizedBloodPressureValue,
        out bool normalizedBloodPressureNotRecorded,
        out int? normalizedPulseValue,
        out bool normalizedPulseNotRecorded)
    {
        var errors = new List<string>();

        // Temperature
        if (temperatureNotRecorded || temperatureValue is null)
        {
            normalizedTemperatureValue = null;
            normalizedTemperatureNotRecorded = true;
        }
        else
        {
            normalizedTemperatureValue = temperatureValue;
            normalizedTemperatureNotRecorded = false;
        }

        // Blood Pressure
        if (bloodPressureNotRecorded || string.IsNullOrWhiteSpace(bloodPressureValue))
        {
            normalizedBloodPressureValue = null;
            normalizedBloodPressureNotRecorded = true;
        }
        else
        {
            normalizedBloodPressureValue = bloodPressureValue.Trim();
            normalizedBloodPressureNotRecorded = false;
        }

        // Pulse
        if (pulseNotRecorded || pulseValue is null)
        {
            normalizedPulseValue = null;
            normalizedPulseNotRecorded = true;
        }
        else
        {
            if (pulseValue < 0)
            {
                errors.Add("Pulse cannot be negative.");
            }

            normalizedPulseValue = pulseValue;
            normalizedPulseNotRecorded = false;
        }

        return errors;
    }

    /// <summary>Trims free text and normalizes blank/whitespace-only input to null — shared by
    /// Complaints and Diagnosis on both create and edit, so the two paths can't silently drift.</summary>
    public static string? NormalizeFreeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Module 5 — validates/normalizes a submitted medication row list into persistable
    /// Medication entities (VisitId/Id/SortOrder/timestamps assigned by the caller). A row with
    /// every field blank is silently dropped (accidental empty row from the add/remove UI, not an
    /// error). A row with a blank Name but at least one other field populated is rejected — Name is
    /// the one required, identifying field per row (approved plan §4, resolved Open Question 2:
    /// "any row where at least one field was touched must have all 5 fields complete").
    /// </summary>
    public static List<string> ValidateMedications(List<MedicationDto>? medications, out List<Medication> normalized)
    {
        var errors = new List<string>();
        normalized = new List<Medication>();

        if (medications is null || medications.Count == 0)
        {
            return errors;
        }

        var sortOrder = 0;
        foreach (var row in medications)
        {
            var name = NormalizeFreeText(row.Name);
            var dosage = NormalizeFreeText(row.Dosage);
            var frequency = NormalizeFreeText(row.Frequency);
            var duration = NormalizeFreeText(row.Duration);
            var instructions = NormalizeFreeText(row.Instructions);

            var anyFieldTouched = name is not null || dosage is not null || frequency is not null
                || duration is not null || instructions is not null;

            if (!anyFieldTouched)
            {
                // Fully-blank row — silently dropped, not an error.
                continue;
            }

            if (name is null || dosage is null || frequency is null || duration is null || instructions is null)
            {
                errors.Add("Each medicine row must have Name, Dosage, Frequency, Duration, and Instructions filled in before saving.");
                continue;
            }

            // Mirrors MedicationConfiguration's HasMaxLength caps so an over-length value is
            // rejected here with a clean 400, not left to fail as an unhandled DbUpdateException.
            var rowErrorCountBefore = errors.Count;

            if (name.Length > 200)
            {
                errors.Add("Medicine name cannot exceed 200 characters.");
            }

            if (dosage.Length > 100)
            {
                errors.Add("Dosage cannot exceed 100 characters.");
            }

            if (frequency.Length > 100)
            {
                errors.Add("Frequency cannot exceed 100 characters.");
            }

            if (duration.Length > 100)
            {
                errors.Add("Duration cannot exceed 100 characters.");
            }

            if (instructions.Length > 500)
            {
                errors.Add("Instructions cannot exceed 500 characters.");
            }

            if (errors.Count > rowErrorCountBefore)
            {
                continue;
            }

            normalized.Add(new Medication
            {
                Name = name,
                Dosage = dosage,
                Frequency = frequency,
                Duration = duration,
                Instructions = instructions,
                SortOrder = sortOrder++
            });
        }

        return errors;
    }
}
