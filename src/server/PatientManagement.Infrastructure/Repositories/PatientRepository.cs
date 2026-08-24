using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly PatientManagementDbContext _dbContext;

    public PatientRepository(PatientManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await _dbContext.Patients.AddAsync(patient, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        // The change tracker already has this entity attached from a prior GetByIdAsync call
        // within the same DbContext scope, so persisting the mutated tracked instance is
        // sufficient — mirrors UserRepository.UpdateAsync's load-mutate-save pattern.
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
