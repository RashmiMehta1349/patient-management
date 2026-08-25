using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PatientManagement.Application.Visits.Services;
using PatientManagement.Domain.Entities;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.Repositories;

public class VisitRepository : IVisitRepository
{
    private readonly PatientManagementDbContext _dbContext;

    public VisitRepository(PatientManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Visit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Visits
            .Include(v => v.Medications.OrderBy(m => m.SortOrder))
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task AddAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        await _dbContext.Visits.AddAsync(visit, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        // Entity is already tracked from a prior GetByIdAsync call in the same DbContext scope —
        // mirrors AppointmentRepository.UpdateAsync's load-mutate-save pattern.
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Visit>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Visits
            .AsNoTracking()
            .Include(v => v.Medications.OrderBy(m => m.SortOrder))
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitDate)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceMedicationsAsync(Guid visitId, IReadOnlyList<Medication> medications, CancellationToken cancellationToken = default)
    {
        // Replace-on-save (approved plan §4): delete the visit's existing medication rows and
        // insert the submitted set within the same DbContext unit of work as the visit's other
        // field updates, so both persist atomically in one SaveChangesAsync call.
        var existing = await _dbContext.Medications.Where(m => m.VisitId == visitId).ToListAsync(cancellationToken);
        _dbContext.Medications.RemoveRange(existing);

        if (medications.Count > 0)
        {
            await _dbContext.Medications.AddRangeAsync(medications, cancellationToken);
        }
    }
}
