using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Appointments;

/// <summary>Shared overlap-error-message formatting for Create/Update, so the two blocking
/// code paths can't drift apart (mirrors AppointmentValidation.cs's precedent).</summary>
public static class AppointmentOverlap
{
    public static async Task<string> BuildErrorMessageAsync(
        IReadOnlyList<Appointment> overlaps,
        IPatientRepository patientRepository,
        CancellationToken cancellationToken)
    {
        var conflict = overlaps[0];
        var conflictPatient = await patientRepository.GetByIdAsync(conflict.PatientId, cancellationToken);
        var conflictPatientName = conflictPatient?.FullName ?? "another patient";
        return $"This time slot overlaps with an existing appointment for {conflictPatientName} at {conflict.AppointmentTime:HH:mm}.";
    }
}
