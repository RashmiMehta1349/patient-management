using PatientMgmt.Domain.Entities;

namespace PatientMgmt.DataAccess.Repositories
{
    public interface IPatientRepository
    {
        Task<Patient> CreateAsync(Patient patient, CancellationToken ct = default);
        Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task UpdateAsync(Patient patient, CancellationToken ct = default);

        /// <summary>Case-insensitive partial match against FullName/PhoneNumber (§3.4, §6).</summary>
        Task<IReadOnlyList<Patient>> SearchAsync(string term, CancellationToken ct = default);

        /// <summary>Advisory duplicate check (B5): exact name + phone match.</summary>
        Task<Patient?> FindPossibleDuplicateAsync(string fullName, string phoneNumber, CancellationToken ct = default);

        /// <summary>Used for server-side PatientCode sequence generation (B4).</summary>
        Task<int> GetPatientCountAsync(CancellationToken ct = default);

        /// <summary>Recent patients hook for Module 7 navigation (task #9).</summary>
        Task<IReadOnlyList<Patient>> GetRecentAsync(int limit, CancellationToken ct = default);
    }
}
