using System;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Auth.Services;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
