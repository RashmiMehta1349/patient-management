namespace PatientManagement.Application.Patients.Dtos;

public class CreatePatientRequestDto
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>ISO 8601 date string (yyyy-MM-dd), e.g. from an HTML date input.</summary>
    public string DateOfBirth { get; set; } = string.Empty;

    /// <summary>One of "Male", "Female", "Other".</summary>
    public string Gender { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}
