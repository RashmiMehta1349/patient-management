namespace PatientManagement.Application.Appointments.Dtos;

/// <summary>Full-edit payload (Increment 3) — reschedule flow (date/time/notes only; PatientId
/// is not changeable via this endpoint).</summary>
public class UpdateAppointmentRequestDto
{
    public string AppointmentDate { get; set; } = string.Empty;

    public string AppointmentTime { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
