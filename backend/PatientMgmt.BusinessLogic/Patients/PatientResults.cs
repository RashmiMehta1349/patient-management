using PatientMgmt.Domain.Entities;

namespace PatientMgmt.BusinessLogic.Patients
{
    public class FieldValidationError
    {
        public string Field { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;

        public FieldValidationError(string field, string message)
        {
            Field = field;
            Message = message;
        }
    }

    public class SavePatientResult
    {
        public bool Success { get; init; }
        public Patient? Patient { get; init; }
        public IReadOnlyList<FieldValidationError> Errors { get; init; } = Array.Empty<FieldValidationError>();

        /// <summary>Advisory only (B5) — never blocks the save; set when the save succeeded despite
        /// an existing same-name+phone record.</summary>
        public bool PossibleDuplicateWarning { get; init; }

        public static SavePatientResult Fail(IReadOnlyList<FieldValidationError> errors) =>
            new() { Success = false, Errors = errors };

        public static SavePatientResult Ok(Patient patient, bool possibleDuplicateWarning = false) =>
            new() { Success = true, Patient = patient, PossibleDuplicateWarning = possibleDuplicateWarning };
    }

    public class DuplicateCheckResult
    {
        public bool PossibleDuplicate { get; init; }
        public Patient? ExistingPatient { get; init; }

        public static DuplicateCheckResult NoMatch() => new() { PossibleDuplicate = false };
        public static DuplicateCheckResult Match(Patient patient) => new() { PossibleDuplicate = true, ExistingPatient = patient };
    }
}
