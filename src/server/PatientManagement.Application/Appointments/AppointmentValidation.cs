using System;
using System.Collections.Generic;
using System.Globalization;

namespace PatientManagement.Application.Appointments;

/// <summary>
/// Shared field-validation logic for Create/Update appointment requests, extracted so the two
/// code paths can't drift apart (mirrors PatientValidation.cs's precedent).
/// </summary>
public static class AppointmentValidation
{
    public static List<string> Validate(long patientId, string appointmentDate, string appointmentTime, out DateOnly date, out TimeOnly time)
    {
        var errors = new List<string>();
        date = default;
        time = default;

        if (patientId == 0L)
        {
            errors.Add("Patient is required.");
        }

        if (string.IsNullOrWhiteSpace(appointmentDate))
        {
            errors.Add("Date is required.");
        }
        else if (!DateOnly.TryParseExact(appointmentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                 && !DateOnly.TryParse(appointmentDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            errors.Add("Date is invalid.");
        }

        if (string.IsNullOrWhiteSpace(appointmentTime))
        {
            errors.Add("Time is required.");
        }
        else if (!TimeOnly.TryParseExact(appointmentTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time)
                 && !TimeOnly.TryParse(appointmentTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
        {
            errors.Add("Time is invalid.");
        }

        return errors;
    }

    public static bool IsValidStatus(string? status) => AppointmentStatuses.IsValid(status);
}
