using PatientMgmt.BusinessLogic.Auth;
using PatientMgmt.BusinessLogic.Interfaces;
using PatientMgmt.DataAccess.Repositories;
using PatientMgmt.Domain.Entities;

namespace PatientMgmt.BusinessLogic.Patients
{
    /// <summary>
    /// Business rules (Module 2 plan §4/§9): required-field validation, DOB/age reconciliation
    /// (B1), Gender enum validation (B2), Patient ID generation (B4), duplicate-warning logic (B5,
    /// advisory-only, never blocks save). Framework-agnostic, unit-testable against a fake repository.
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IClock _clock;

        public PatientService(IPatientRepository patientRepository, IClock clock)
        {
            _patientRepository = patientRepository;
            _clock = clock;
        }

        public async Task<SavePatientResult> CreateAsync(
            string fullName, DateTime? dateOfBirth, int? approxAgeAtEntry, string gender,
            string phoneNumber, string? email, string? address, CancellationToken ct = default)
        {
            var now = _clock.UtcNow;
            var errors = Validate(fullName, dateOfBirth, approxAgeAtEntry, gender, phoneNumber, email, now);
            if (errors.Count > 0)
                return SavePatientResult.Fail(errors);

            var genderEnum = Enum.Parse<Gender>(gender, ignoreCase: true);

            // B5: advisory-only duplicate check; never blocks the save.
            var duplicate = await _patientRepository.FindPossibleDuplicateAsync(fullName, phoneNumber, ct);

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                PatientCode = await GeneratePatientCodeAsync(ct),
                FullName = fullName.Trim(),
                DateOfBirth = dateOfBirth,
                ApproxAgeAtEntry = dateOfBirth is null ? approxAgeAtEntry : null,
                EntryDate = dateOfBirth is null ? now : null,
                Gender = genderEnum,
                PhoneNumber = phoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            await _patientRepository.CreateAsync(patient, ct);
            return SavePatientResult.Ok(patient, possibleDuplicateWarning: duplicate is not null);
        }

        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            _patientRepository.GetByIdAsync(id, ct);

        public async Task<SavePatientResult> UpdateAsync(
            Guid id, string fullName, DateTime? dateOfBirth, int? approxAgeAtEntry, string gender,
            string phoneNumber, string? email, string? address, CancellationToken ct = default)
        {
            var now = _clock.UtcNow;
            var errors = Validate(fullName, dateOfBirth, approxAgeAtEntry, gender, phoneNumber, email, now);
            if (errors.Count > 0)
                return SavePatientResult.Fail(errors);

            var patient = await _patientRepository.GetByIdAsync(id, ct);
            if (patient is null)
                return SavePatientResult.Fail(new[] { new FieldValidationError("id", "Patient not found.") });

            var genderEnum = Enum.Parse<Gender>(gender, ignoreCase: true);

            // Edit does not re-run the duplicate check (§3.2) — editing an existing patient isn't
            // a new-duplicate scenario.
            patient.FullName = fullName.Trim();
            patient.DateOfBirth = dateOfBirth;
            patient.ApproxAgeAtEntry = dateOfBirth is null ? approxAgeAtEntry : null;
            patient.EntryDate = dateOfBirth is null ? (patient.EntryDate ?? now) : null;
            patient.Gender = genderEnum;
            patient.PhoneNumber = phoneNumber.Trim();
            patient.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            patient.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
            patient.UpdatedAt = now;

            await _patientRepository.UpdateAsync(patient, ct);
            return SavePatientResult.Ok(patient);
        }

        public Task<IReadOnlyList<Patient>> SearchAsync(string term, CancellationToken ct = default) =>
            _patientRepository.SearchAsync(term ?? string.Empty, ct);

        public async Task<DuplicateCheckResult> CheckDuplicateAsync(string fullName, string phoneNumber, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber))
                return DuplicateCheckResult.NoMatch();

            var existing = await _patientRepository.FindPossibleDuplicateAsync(fullName, phoneNumber, ct);
            return existing is null ? DuplicateCheckResult.NoMatch() : DuplicateCheckResult.Match(existing);
        }

        public Task<IReadOnlyList<Patient>> GetRecentAsync(int limit, CancellationToken ct = default) =>
            _patientRepository.GetRecentAsync(limit, ct);

        private static List<FieldValidationError> Validate(
            string fullName, DateTime? dateOfBirth, int? approxAgeAtEntry, string gender,
            string phoneNumber, string? email, DateTime now)
        {
            var errors = new List<FieldValidationError>();

            if (string.IsNullOrWhiteSpace(fullName))
                errors.Add(new FieldValidationError("fullName", "Full name is required."));

            if (string.IsNullOrWhiteSpace(phoneNumber))
                errors.Add(new FieldValidationError("phoneNumber", "Phone number is required."));
            else if (!IsValidPhone(phoneNumber))
                errors.Add(new FieldValidationError("phoneNumber", "Phone number format is invalid."));

            if (string.IsNullOrWhiteSpace(gender) || !Enum.TryParse<Gender>(gender, ignoreCase: true, out _))
                errors.Add(new FieldValidationError("gender", "Gender must be one of: Male, Female, Other."));

            // B1: at least one of DateOfBirth/ApproxAgeAtEntry required.
            if (dateOfBirth is null && approxAgeAtEntry is null)
                errors.Add(new FieldValidationError("dateOfBirth", "Either Date of Birth or an approximate Age is required."));

            if (dateOfBirth is not null && dateOfBirth.Value.Date > now.Date)
                errors.Add(new FieldValidationError("dateOfBirth", "Date of Birth cannot be in the future."));

            if (approxAgeAtEntry is not null && (approxAgeAtEntry < 0 || approxAgeAtEntry > 150))
                errors.Add(new FieldValidationError("approxAgeAtEntry", "Age must be between 0 and 150."));

            if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                errors.Add(new FieldValidationError("email", "Email format is invalid."));

            return errors;
        }

        private static bool IsValidPhone(string phone)
        {
            // B7: basic format validation — digits, optional leading '+', min/max length.
            var trimmed = phone.Trim();
            var digits = trimmed.StartsWith('+') ? trimmed[1..] : trimmed;
            return digits.Length >= 7 && digits.Length <= 15 && digits.All(char.IsDigit);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> GeneratePatientCodeAsync(CancellationToken ct)
        {
            // B4: simple server-side sequence; sufficient for the BRD's single-writer,
            // single-instance deployment (see plan §14 Risks for the multi-instance caveat).
            var count = await _patientRepository.GetPatientCountAsync(ct);
            return $"P-{count + 1:D5}";
        }
    }
}
