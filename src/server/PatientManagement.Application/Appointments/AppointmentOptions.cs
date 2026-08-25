namespace PatientManagement.Application.Appointments;

/// <summary>
/// Bound from appsettings.json "Appointments" section. SlotMinutes is the assumed fixed slot
/// length used for overlap detection since no end-time/duration field is captured on Appointment
/// (approved plan §2/§4 — Open Question 1, resolved: 30-minute default).
/// </summary>
public class AppointmentOptions
{
    public const string SectionName = "Appointments";

    public int SlotMinutes { get; set; } = 30;
}
