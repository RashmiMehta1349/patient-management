using System;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Patients.Services;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
}
