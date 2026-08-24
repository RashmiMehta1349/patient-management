using System.ComponentModel.DataAnnotations;
using PatientMgmt.Domain.Entities;

namespace PatientMgmt.Domain.Contracts
{
    /// <summary>
    /// Request to register a new patient. Either DateOfBirth or ApproxAgeAtEntry must be supplied
    /// (B1) — cross-field rule enforced in the business-logic tier, not via attributes alone.
    /// </summary>
    public record CreatePatientRequest(
        [property: Required, StringLength(200, MinimumLength = 1)] string FullName,
        DateTime? DateOfBirth,
        [property: Range(0, 150)] int? ApproxAgeAtEntry,
        [property: Required] string Gender,
        [property: Required, StringLength(20, MinimumLength = 1)] string PhoneNumber,
        [property: EmailAddress, StringLength(256)] string? Email,
        [property: StringLength(500)] string? Address
    );

    public record UpdatePatientRequest(
        [property: Required, StringLength(200, MinimumLength = 1)] string FullName,
        DateTime? DateOfBirth,
        [property: Range(0, 150)] int? ApproxAgeAtEntry,
        [property: Required] string Gender,
        [property: Required, StringLength(20, MinimumLength = 1)] string PhoneNumber,
        [property: EmailAddress, StringLength(256)] string? Email,
        [property: StringLength(500)] string? Address
    );

    public record PatientResponse(
        Guid Id,
        string PatientCode,
        string FullName,
        DateTime? DateOfBirth,
        int? ApproxAgeAtEntry,
        DateTime? EntryDate,
        string Gender,
        string PhoneNumber,
        string? Email,
        string? Address,
        DateTime CreatedAt,
        DateTime UpdatedAt
    )
    {
        public static PatientResponse FromEntity(Patient p) => new(
            p.Id,
            p.PatientCode,
            p.FullName,
            p.DateOfBirth,
            p.ApproxAgeAtEntry,
            p.EntryDate,
            p.Gender.ToString(),
            p.PhoneNumber,
            p.Email,
            p.Address,
            p.CreatedAt,
            p.UpdatedAt);
    }

    public record DuplicateCheckResponse(bool PossibleDuplicate, Guid? ExistingPatientId, string? ExistingPatientCode);

    /// <summary>Field-level validation error shape for 400 responses (API tier).</summary>
    public record FieldError(string Field, string Message);

    public record ValidationErrorResponse(string Message, IReadOnlyList<FieldError> Errors);
}
