using Microsoft.EntityFrameworkCore;
using PatientMgmt.Domain.Entities;

namespace PatientMgmt.DataAccess.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly AppDbContext _db;

        public PatientRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Patient> CreateAsync(Patient patient, CancellationToken ct = default)
        {
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync(ct);
            return patient;
        }

        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            _db.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);

        public async Task UpdateAsync(Patient patient, CancellationToken ct = default)
        {
            _db.Patients.Update(patient);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<Patient>> SearchAsync(string term, CancellationToken ct = default)
        {
            var normalized = term.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(normalized))
                return Array.Empty<Patient>();

            return await _db.Patients
                .Where(p => p.FullName.ToLower().Contains(normalized) || p.PhoneNumber.ToLower().Contains(normalized))
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync(ct);
        }

        public Task<Patient?> FindPossibleDuplicateAsync(string fullName, string phoneNumber, CancellationToken ct = default)
        {
            var normalizedName = fullName.Trim().ToLowerInvariant();
            var normalizedPhone = phoneNumber.Trim();
            return _db.Patients.FirstOrDefaultAsync(
                p => p.FullName.ToLower() == normalizedName && p.PhoneNumber == normalizedPhone, ct);
        }

        public Task<int> GetPatientCountAsync(CancellationToken ct = default) =>
            _db.Patients.CountAsync(ct);

        public async Task<IReadOnlyList<Patient>> GetRecentAsync(int limit, CancellationToken ct = default)
        {
            return await _db.Patients
                .OrderByDescending(p => p.UpdatedAt)
                .Take(limit)
                .ToListAsync(ct);
        }
    }
}
