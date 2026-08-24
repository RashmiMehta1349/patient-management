using PatientMgmt.Domain.Entities;

namespace PatientMgmt.DataAccess.Repositories
{
    public interface ISessionRepository
    {
        Task<Session> CreateAsync(Session session, CancellationToken ct = default);
        Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task UpdateLastActivityAsync(Guid id, DateTime lastActivityUtc, CancellationToken ct = default);
        Task InvalidateAsync(Guid id, CancellationToken ct = default);
        Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default);
    }
}
