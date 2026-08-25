using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Patients.Services;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);

    /// <summary>Browse-all: one page of all patients ordered by FullName ascending, plus the
    /// total row count (Increment 3 revision, §9b.1).</summary>
    Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive partial match against FullName and PhoneNumber, paginated,
    /// ordered by FullName ascending (Increment 3, §9b.1).</summary>
    Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default);
}
