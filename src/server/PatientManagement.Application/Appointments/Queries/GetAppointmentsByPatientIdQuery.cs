using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Appointments.Dtos;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Patients.Services;

namespace PatientManagement.Application.Appointments.Queries;

/// <summary>Patient-scoped appointment list (Increment 3) — powers the Patient Detail
/// "Appointments" tab cross-navigation. Empty array (not 404) when the patient has none.</summary>
public class GetAppointmentsByPatientIdQueryHandler
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;

    public GetAppointmentsByPatientIdQueryHandler(IAppointmentRepository appointmentRepository, IPatientRepository patientRepository)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
    }

    public async Task<IReadOnlyList<AppointmentDto>> HandleAsync(long patientId, CancellationToken cancellationToken = default)
    {
        var appointments = await _appointmentRepository.GetByPatientIdAsync(patientId, cancellationToken);
        if (appointments.Count == 0)
        {
            return new List<AppointmentDto>();
        }

        var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
        var patientName = patient?.FullName ?? string.Empty;

        return appointments.Select(a => AppointmentMapper.ToDtoWithoutOverlapCheck(a, patientName)).ToList();
    }
}
