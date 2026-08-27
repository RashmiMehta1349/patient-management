namespace PatientManagement.Application.Patients.Dtos;

/// <summary>Full-payload PUT body — identical shape to CreatePatientRequestDto; Id comes from the route.</summary>
public class UpdatePatientRequestDto
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>ISO 8601 date string (yyyy-MM-dd), e.g. from an HTML date input.</summary>
    public string DateOfBirth { get; set; } = string.Empty;

    /// <summary>One of "Male", "Female", "Other".</summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>International dial code, e.g. "+91".</summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>National number only (no dial code), max 10 digits.</summary>
    public string PhoneNumber { get; set; } = string.Empty;
}
