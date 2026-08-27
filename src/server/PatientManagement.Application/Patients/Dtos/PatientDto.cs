using System;

namespace PatientManagement.Application.Patients.Dtos;

public class PatientDto
{
    public long Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>ISO 8601 date string (yyyy-MM-dd).</summary>
    public string DateOfBirth { get; set; } = string.Empty;

    /// <summary>Computed on read from DateOfBirth — never persisted.</summary>
    public int Age { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
