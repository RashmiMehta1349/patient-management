using System;

namespace PatientMgmt.Domain.Entities
{
    /// <summary>
    /// Constrained gender value set (B2) — structured/searchable/reportable rather than free text.
    /// </summary>
    public enum Gender
    {
        Male = 0,
        Female = 1,
        Other = 2
    }

    /// <summary>
    /// Root clinical entity (Module 2). Every appointment, consultation, prescription, and
    /// history record hangs off a Patient row. No delete capability (Module spec §5) — edit-only
    /// correction of erroneous entries.
    /// </summary>
    public class Patient
    {
        public Guid Id { get; set; }

        /// <summary>Human-readable ID (B4), e.g. "P-00001"; generated server-side at insert.</summary>
        public string PatientCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        /// <summary>Nullable to allow age-only entry (B1); at least one of DateOfBirth/ApproxAgeAtEntry
        /// must be present — enforced in the business-logic tier.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Captured when the doctor enters age instead of DOB (B1); paired with EntryDate.</summary>
        public int? ApproxAgeAtEntry { get; set; }

        /// <summary>Date the ApproxAgeAtEntry was captured, so age can be derived later without
        /// pretending to know an exact DOB.</summary>
        public DateTime? EntryDate { get; set; }

        public Gender Gender { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
