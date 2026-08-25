namespace PatientManagement.Application.Appointments;

/// <summary>Fixed picklist values for Appointment.Status — no additional sub-statuses
/// (Modules\03 §5). Mirrors PatientGenders.cs's constants-class precedent.</summary>
public static class AppointmentStatuses
{
    public const string Scheduled = "Scheduled";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string NoShow = "NoShow";

    public static readonly string[] AllowedValues = { Scheduled, Completed, Cancelled, NoShow };

    public static bool IsValid(string? value) =>
        value is not null && System.Array.IndexOf(AllowedValues, value) >= 0;

    /// <summary>Statuses excluded from overlap-conflict comparison — a cancelled/no-show slot is
    /// not really "occupying" the day (approved plan §3.4 step 4, an explicit interpretation).</summary>
    public static readonly string[] ExcludedFromOverlapCheck = { Cancelled, NoShow };
}
