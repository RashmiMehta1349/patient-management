using PatientMgmt.Domain.Entities;

namespace PatientMgmt.DataAccess.Repositories
{
    public interface IPasswordResetTokenRepository
    {
        Task<PasswordResetToken> CreateAsync(PasswordResetToken token, CancellationToken ct = default);
        Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
        Task MarkUsedAsync(Guid id, DateTime usedAtUtc, CancellationToken ct = default);
    }
}
