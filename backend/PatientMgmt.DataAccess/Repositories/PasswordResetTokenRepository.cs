using Microsoft.EntityFrameworkCore;
using PatientMgmt.Domain.Entities;

namespace PatientMgmt.DataAccess.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly AppDbContext _db;

        public PasswordResetTokenRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token, CancellationToken ct = default)
        {
            _db.PasswordResetTokens.Add(token);
            await _db.SaveChangesAsync(ct);
            return token;
        }

        public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
            _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        public async Task MarkUsedAsync(Guid id, DateTime usedAtUtc, CancellationToken ct = default)
        {
            var token = await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (token is null) return;
            token.UsedAt = usedAtUtc;
            await _db.SaveChangesAsync(ct);
        }
    }
}
