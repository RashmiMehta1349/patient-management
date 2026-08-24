using Microsoft.EntityFrameworkCore;
using PatientMgmt.Domain.Entities;

namespace PatientMgmt.DataAccess.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        public Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken ct = default)
        {
            var normalized = usernameOrEmail.Trim().ToLowerInvariant();
            return _db.Users.FirstOrDefaultAsync(
                u => u.Email.ToLower() == normalized || (u.Username != null && u.Username.ToLower() == normalized),
                ct);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
        }

        public async Task UpdatePasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return;
            user.PasswordHash = newPasswordHash;
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateLastLoginAsync(Guid userId, DateTime loginTimeUtc, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return;
            user.LastLoginAt = loginTimeUtc;
            await _db.SaveChangesAsync(ct);
        }
    }
}
