using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Appointments.Queries;
using PatientManagement.Application.DataExport.Dtos;
using PatientManagement.Application.Patients.Queries;
using PatientManagement.Application.Visits.Queries;

namespace PatientManagement.Application.DataExport.Queries;

/// <summary>
/// Composes a single patient's profile — plus, only when requested, summarized visit-history and
/// appointment sections — into export shape (plan §10 task 8). Reuses GetPatientByIdQueryHandler/
/// PatientDto, GetVisitsByPatientIdQueryHandler/VisitDto[], and
/// GetAppointmentsByPatientIdQueryHandler/AppointmentDto[] (all already built) rather than
/// introducing any new repository access. includeHistory defaults to false (plan §5 Open Question 3
/// — profile-only by default, opt-in via the caller-supplied flag). A patient with zero visits/
/// appointments and includeHistory=true yields empty (not error) sections.
/// </summary>
public class GetPatientExportQueryHandler
{
    private readonly GetPatientByIdQueryHandler _getPatientByIdHandler;
    private readonly GetVisitsByPatientIdQueryHandler _getVisitsByPatientIdHandler;
    private readonly GetAppointmentsByPatientIdQueryHandler _getAppointmentsByPatientIdHandler;

    public GetPatientExportQueryHandler(
        GetPatientByIdQueryHandler getPatientByIdHandler,
        GetVisitsByPatientIdQueryHandler getVisitsByPatientIdHandler,
        GetAppointmentsByPatientIdQueryHandler getAppointmentsByPatientIdHandler)
    {
        _getPatientByIdHandler = getPatientByIdHandler;
        _getVisitsByPatientIdHandler = getVisitsByPatientIdHandler;
        _getAppointmentsByPatientIdHandler = getAppointmentsByPatientIdHandler;
    }

    public async Task<PatientExportDto?> HandleAsync(long patientId, bool includeHistory = false, CancellationToken cancellationToken = default)
    {
        var patient = await _getPatientByIdHandler.HandleAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return null;
        }

        List<VisitSummaryDto>? visitSummaries = null;
        List<AppointmentSummaryDto>? appointmentSummaries = null;
        if (includeHistory)
        {
            var visitsResult = await _getVisitsByPatientIdHandler.HandleAsync(patientId, cancellationToken: cancellationToken);
            var visits = visitsResult.Succeeded ? visitsResult.Value! : new List<Visits.Dtos.VisitDto>();

            visitSummaries = visits
                .Select(v => new VisitSummaryDto
                {
                    VisitDate = v.VisitDate,
                    TemperatureDisplay = v.TemperatureNotRecorded ? "Not recorded" : v.TemperatureValue?.ToString() ?? "Not recorded",
                    BloodPressureDisplay = v.BloodPressureNotRecorded ? "Not recorded" : v.BloodPressureValue ?? "Not recorded",
                    PulseDisplay = v.PulseNotRecorded ? "Not recorded" : v.PulseValue?.ToString() ?? "Not recorded",
                    Diagnosis = v.Diagnosis,
                    Medications = v.Medications
                        .Select(m => new MedicationExportDto
                        {
                            Name = m.Name,
                            Dosage = m.Dosage,
                            Frequency = m.Frequency,
                            Duration = m.Duration,
                            Instructions = m.Instructions
                        })
                        .ToList()
                })
                .ToList();

            var appointments = await _getAppointmentsByPatientIdHandler.HandleAsync(patientId, cancellationToken);
            appointmentSummaries = appointments
                .Select(a => new AppointmentSummaryDto
                {
                    AppointmentDate = a.AppointmentDate,
                    AppointmentTime = a.AppointmentTime,
                    Status = a.Status,
                    Notes = a.Notes
                })
                .ToList();
        }

        return new PatientExportDto
        {
            PatientId = patient.Id,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            Age = patient.Age,
            Gender = patient.Gender,
            PhoneNumber = $"{patient.CountryCode}{patient.PhoneNumber}",
            RegisteredAt = patient.CreatedAt,
            VisitSummaries = visitSummaries,
            AppointmentSummaries = appointmentSummaries
        };
    }
}
