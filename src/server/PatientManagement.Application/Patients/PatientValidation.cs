using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PatientManagement.Application.Patients;

/// <summary>
/// Shared field-validation logic for Create and Update patient requests, extracted so the two
/// code paths can't drift apart (Planning\02_Patient_Management_Plan.md §9a.4).
/// </summary>
public static class PatientValidation
{
    private static readonly Regex CountryCodePattern = new("^\\+[0-9]{1,4}$", RegexOptions.Compiled);
    private static readonly Regex PhoneNumberPattern = new("^[0-9]{1,10}$", RegexOptions.Compiled);

    public static List<string> Validate(string fullName, string dateOfBirth, string gender, string countryCode, string phoneNumber, DateTime utcNow, out DateOnly dob)
    {
        var errors = new List<string>();
        dob = default;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(dateOfBirth))
        {
            errors.Add("Date of birth is required.");
        }
        else if (!DateOnly.TryParseExact(dateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dob)
                 && !DateOnly.TryParse(dateOfBirth, CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
        {
            errors.Add("Date of birth is invalid.");
        }
        else if (dob > DateOnly.FromDateTime(utcNow))
        {
            errors.Add("Date of birth cannot be in the future.");
        }

        if (string.IsNullOrWhiteSpace(gender))
        {
            errors.Add("Gender is required.");
        }
        else if (!PatientGenders.IsValid(gender.Trim()))
        {
            errors.Add("Gender must be one of: Male, Female, Other.");
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            errors.Add("Country code is required.");
        }
        else if (!CountryCodePattern.IsMatch(countryCode.Trim()))
        {
            errors.Add("Country code must be a valid dial code, e.g. +91.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            errors.Add("Phone number is required.");
        }
        else if (!PhoneNumberPattern.IsMatch(phoneNumber.Trim()))
        {
            errors.Add("Phone number must be numeric and at most 10 digits.");
        }

        return errors;
    }
}
