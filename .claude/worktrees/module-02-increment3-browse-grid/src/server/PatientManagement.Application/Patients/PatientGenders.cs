namespace PatientManagement.Application.Patients;

/// <summary>Fixed picklist values for Patient.Gender, per Product Owner sign-off (plan §Open Questions #1).</summary>
public static class PatientGenders
{
    public const string Male = "Male";
    public const string Female = "Female";
    public const string Other = "Other";

    public static readonly string[] AllowedValues = { Male, Female, Other };

    public static bool IsValid(string? value) =>
        value is not null && System.Array.IndexOf(AllowedValues, value) >= 0;
}
