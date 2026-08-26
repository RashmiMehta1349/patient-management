using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Domain.Entities;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly PatientManagementDbContext _dbContext;

    public AppointmentRepository(PatientManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Appointment?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _dbContext.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Appointments.AddAsync(appointment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        // Entity is already tracked from a prior GetByIdAsync call in the same DbContext scope —
        // mirrors PatientRepository.UpdateAsync's load-mutate-save pattern.
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate == date)
            .OrderBy(a => a.AppointmentTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(long patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetOverlappingAsync(
        DateOnly date,
        TimeOnly time,
        int slotMinutes,
        long? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var windowStart = time.AddMinutes(-slotMinutes);
        var windowEnd = time.AddMinutes(slotMinutes);

        var query = _dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate == date)
            .Where(a => !AppointmentStatuses.ExcludedFromOverlapCheck.Contains(a.Status));

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAppointmentId.Value);
        }

        var candidates = await query.ToListAsync(cancellationToken);

        // TimeOnly arithmetic across midnight (windowStart > windowEnd wrap-around) isn't
        // expected for a single-day clinic slot window, so a simple inclusive range comparison
        // is sufficient here; done in-memory since TimeOnly comparisons don't translate to SQL
        // reliably across all EF providers/tests (SQLite in integration tests).
        return candidates
            .Where(a => a.AppointmentTime >= windowStart && a.AppointmentTime <= windowEnd)
            .ToList();
    }
}
