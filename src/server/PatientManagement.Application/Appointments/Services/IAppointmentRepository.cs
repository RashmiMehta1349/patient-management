using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Appointments.Services;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);

    /// <summary>All appointments for a given date, ordered by AppointmentTime ascending.</summary>
    Task<IReadOnlyList<Appointment>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>All appointments for a given patient, ordered by date/time (Increment 3).</summary>
    Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appointments on the same date whose start time is within slotMinutes of the given time,
    /// excluding Cancelled/NoShow rows (approved plan §3.4 step 4) and excluding
    /// excludeAppointmentId (used when re-checking an edit against itself, Increment 3).
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetOverlappingAsync(
        DateOnly date,
        TimeOnly time,
        int slotMinutes,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);
}
