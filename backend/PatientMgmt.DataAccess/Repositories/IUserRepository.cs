using PatientMgmt.Domain.Entities;

namespace PatientMgmt.DataAccess.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task UpdatePasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken ct = default);
        Task UpdateLastLoginAsync(Guid userId, DateTime loginTimeUtc, CancellationToken ct = default);
    }
}
