using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Appointments.Dtos;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Appointments;

/// <summary>
/// Shared Appointment -> AppointmentDto mapping, reused by all command/query handlers so the
/// response shape (and overlap-warning annotation logic) can't drift apart across create/update/
/// list/get-by-id (mirrors CreatePatientCommandHandler.ToDto's precedent).
/// </summary>
public static class AppointmentMapper
{
    public static AppointmentDto ToDto(Appointment appointment, string patientName, IReadOnlyList<Appointment> overlaps, IReadOnlyDictionary<long, string> patientNamesByPatientId)
    {
        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = patientName,
            AppointmentDate = appointment.AppointmentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            AppointmentTime = appointment.AppointmentTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            Status = appointment.Status,
            Notes = appointment.Notes,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt,
            HasOverlapWarning = overlaps.Count > 0,
            ConflictingAppointments = overlaps.Select(o => new ConflictingAppointmentDto
            {
                Id = o.Id,
                PatientName = patientNamesByPatientId.TryGetValue(o.PatientId, out var name) ? name : string.Empty,
                AppointmentTime = o.AppointmentTime.ToString("HH:mm", CultureInfo.InvariantCulture)
            }).ToList()
        };
    }

    /// <summary>Convenience overload for single-appointment handlers (create/update/status-update)
    /// that resolves conflicting appointments' patient names via one-off lookups.</summary>
    public static async Task<AppointmentDto> ToDtoAsync(
        Appointment appointment,
        string patientName,
        IReadOnlyList<Appointment> overlaps,
        IPatientRepository patientRepository,
        CancellationToken cancellationToken)
    {
        var namesByPatientId = new Dictionary<long, string>();
        foreach (var overlap in overlaps)
        {
            if (namesByPatientId.ContainsKey(overlap.PatientId))
            {
                continue;
            }

            var overlapPatient = await patientRepository.GetByIdAsync(overlap.PatientId, cancellationToken);
            namesByPatientId[overlap.PatientId] = overlapPatient?.FullName ?? string.Empty;
        }

        return ToDto(appointment, patientName, overlaps, namesByPatientId);
    }

    /// <summary>No-overlap-check convenience overload for list/get-by-id reads, where the
    /// overlap flag is not applicable/computed.</summary>
    public static AppointmentDto ToDtoWithoutOverlapCheck(Appointment appointment, string patientName)
    {
        return ToDto(appointment, patientName, new List<Appointment>(), new Dictionary<long, string>());
    }
}
