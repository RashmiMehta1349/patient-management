using System;

namespace PatientManagement.Domain.Entities;

/// <summary>
/// A scheduled visit tied to an existing Patient. Every Appointment requires a valid PatientId
/// (R5) — there is no anonymous/placeholder appointment. Date and time are stored separately
/// (AppointmentDate / AppointmentTime) to support efficient day-scoped querying for the Daily
/// List screen (§5 of the approved plan).
/// </summary>
public class Appointment
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    /// <summary>Start time only — no explicit end time/duration is captured (approved plan §2/§4
    /// assumption). Overlap detection uses a fixed configurable slot length instead.</summary>
    public TimeOnly AppointmentTime { get; set; }

    /// <summary>One of AppointmentStatuses.AllowedValues (Scheduled/Completed/Cancelled/NoShow).</summary>
    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
