using PatientMgmt.BusinessLogic.Patients;
using PatientMgmt.Domain.Entities;

namespace PatientMgmt.BusinessLogic.Interfaces
{
    public interface IPatientService
    {
        Task<SavePatientResult> CreateAsync(
            string fullName, DateTime? dateOfBirth, int? approxAgeAtEntry, string gender,
            string phoneNumber, string? email, string? address, CancellationToken ct = default);

        Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<SavePatientResult> UpdateAsync(
            Guid id, string fullName, DateTime? dateOfBirth, int? approxAgeAtEntry, string gender,
            string phoneNumber, string? email, string? address, CancellationToken ct = default);

        Task<IReadOnlyList<Patient>> SearchAsync(string term, CancellationToken ct = default);

        Task<DuplicateCheckResult> CheckDuplicateAsync(string fullName, string phoneNumber, CancellationToken ct = default);

        Task<IReadOnlyList<Patient>> GetRecentAsync(int limit, CancellationToken ct = default);
    }
}
