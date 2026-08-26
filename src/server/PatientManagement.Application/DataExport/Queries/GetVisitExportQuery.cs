using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.DataExport.Dtos;
using PatientManagement.Application.Visits.Queries;

namespace PatientManagement.Application.DataExport.Queries;

/// <summary>
/// Composes a single visit into export shape (plan §10 task 2) by reusing the existing
/// GetVisitByIdQueryHandler/VisitDto (Module 6) rather than re-querying the repository directly —
/// no new repository method needed. Not-found is a normal, expected outcome, mirroring
/// GetVisitByIdQueryHandler's own null-return convention.
/// </summary>
public class GetVisitExportQueryHandler
{
    private readonly GetVisitByIdQueryHandler _getVisitByIdHandler;

    public GetVisitExportQueryHandler(GetVisitByIdQueryHandler getVisitByIdHandler)
    {
        _getVisitByIdHandler = getVisitByIdHandler;
    }

    public async Task<VisitExportDto?> HandleAsync(long visitId, CancellationToken cancellationToken = default)
    {
        var visit = await _getVisitByIdHandler.HandleAsync(visitId, cancellationToken);
        if (visit is null)
        {
            return null;
        }

        return new VisitExportDto
        {
            VisitId = visit.Id,
            PatientName = visit.PatientName,
            VisitDate = visit.VisitDate,
            TemperatureValue = visit.TemperatureValue,
            TemperatureNotRecorded = visit.TemperatureNotRecorded,
            BloodPressureValue = visit.BloodPressureValue,
            BloodPressureNotRecorded = visit.BloodPressureNotRecorded,
            PulseValue = visit.PulseValue,
            PulseNotRecorded = visit.PulseNotRecorded,
            Complaints = visit.Complaints,
            Diagnosis = visit.Diagnosis,
            Medications = visit.Medications
                .Select(m => new MedicationExportDto
                {
                    Name = m.Name,
                    Dosage = m.Dosage,
                    Frequency = m.Frequency,
                    Duration = m.Duration,
                    Instructions = m.Instructions
                })
                .ToList()
        };
    }
}
