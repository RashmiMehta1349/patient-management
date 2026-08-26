using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Appointments.Dtos;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Patients.Services;

namespace PatientManagement.Application.Appointments.Queries;

/// <summary>
/// Returns the given date's appointments, ordered by AppointmentTime ascending, hydrated with
/// each row's Patient.FullName. Unpaginated by design — a single clinic day's volume is
/// inherently small (approved plan §4).
/// </summary>
public class GetAppointmentsByDateQueryHandler
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;

    public GetAppointmentsByDateQueryHandler(IAppointmentRepository appointmentRepository, IPatientRepository patientRepository)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
    }

    public async Task<IReadOnlyList<AppointmentDto>> HandleAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var appointments = await _appointmentRepository.GetByDateAsync(date, cancellationToken);
        if (appointments.Count == 0)
        {
            return new List<AppointmentDto>();
        }

        var patientNames = await ResolvePatientNamesAsync(appointments.Select(a => a.PatientId), cancellationToken);

        return appointments
            .Select(a => AppointmentMapper.ToDtoWithoutOverlapCheck(a, patientNames.TryGetValue(a.PatientId, out var name) ? name : string.Empty))
            .ToList();
    }

    internal async Task<Dictionary<long, string>> ResolvePatientNamesAsync(IEnumerable<long> patientIds, CancellationToken cancellationToken)
    {
        var result = new Dictionary<long, string>();
        foreach (var patientId in patientIds.Distinct())
        {
            var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
            result[patientId] = patient?.FullName ?? string.Empty;
        }

        return result;
    }
}
