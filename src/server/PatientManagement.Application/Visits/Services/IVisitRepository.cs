using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Visits.Services;

public interface IVisitRepository
{
    Task<Visit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Visit visit, CancellationToken cancellationToken = default);

    Task UpdateAsync(Visit visit, CancellationToken cancellationToken = default);

    /// <summary>All visits for a given patient, most-recent-first (patient-scoped Consultations list).</summary>
    Task<IReadOnlyList<Visit>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Stages a full replace-on-save of a visit's medication set (delete existing, insert
    /// submitted) within the current DbContext unit of work — does not call SaveChanges itself, so
    /// it can be combined with the visit's own field update in a single atomic save (approved plan
    /// §4 Transactionality).</summary>
    Task ReplaceMedicationsAsync(Guid visitId, IReadOnlyList<Medication> medications, CancellationToken cancellationToken = default);
}
