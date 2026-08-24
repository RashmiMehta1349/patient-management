using Microsoft.EntityFrameworkCore;
using PatientMgmt.Domain.Entities;

namespace PatientMgmt.DataAccess.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly AppDbContext _db;

        public SessionRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Session> CreateAsync(Session session, CancellationToken ct = default)
        {
            _db.Sessions.Add(session);
            await _db.SaveChangesAsync(ct);
            return session;
        }

        public Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            _db.Sessions.FirstOrDefaultAsync(s => s.Id == id, ct);

        public async Task UpdateLastActivityAsync(Guid id, DateTime lastActivityUtc, CancellationToken ct = default)
        {
            var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (session is null) return;
            session.LastActivityAt = lastActivityUtc;
            await _db.SaveChangesAsync(ct);
        }

        public async Task InvalidateAsync(Guid id, CancellationToken ct = default)
        {
            var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (session is null) return;
            session.IsValid = false;
            await _db.SaveChangesAsync(ct);
        }

        public async Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default)
        {
            var sessions = await _db.Sessions.Where(s => s.UserId == userId && s.IsValid).ToListAsync(ct);
            foreach (var s in sessions)
            {
                s.IsValid = false;
            }
            await _db.SaveChangesAsync(ct);
        }
    }
}
