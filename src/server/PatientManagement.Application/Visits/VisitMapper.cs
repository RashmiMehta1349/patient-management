using System.Linq;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Visits;

/// <summary>Shared Visit -> VisitDto mapping, reused by all command/query handlers so the response
/// shape can't drift apart across create/update/list/get-by-id (mirrors AppointmentMapper's precedent).</summary>
public static class VisitMapper
{
    public static VisitDto ToDto(Visit visit, string patientName)
    {
        return new VisitDto
        {
            Id = visit.Id,
            PatientId = visit.PatientId,
            PatientName = patientName,
            AppointmentId = visit.AppointmentId,
            VisitDate = visit.VisitDate,
            TemperatureValue = visit.TemperatureValue,
            TemperatureNotRecorded = visit.TemperatureNotRecorded,
            BloodPressureValue = visit.BloodPressureValue,
            BloodPressureNotRecorded = visit.BloodPressureNotRecorded,
            PulseValue = visit.PulseValue,
            PulseNotRecorded = visit.PulseNotRecorded,
            Complaints = visit.Complaints,
            Diagnosis = visit.Diagnosis,
            CreatedAt = visit.CreatedAt,
            UpdatedAt = visit.UpdatedAt,
            Medications = visit.Medications
                .OrderBy(m => m.SortOrder)
                .Select(m => new MedicationDto
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
