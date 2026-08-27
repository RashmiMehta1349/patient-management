using System;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Appointments;

/// <summary>
/// Auto-transitions a stale "Scheduled" appointment to "NoShow" once its date has passed without
/// a recorded visit — CreateVisitCommandHandler already flips a linked appointment to Completed
/// when a consultation happens; this covers the complementary "never showed up" case. Applied
/// lazily at read time by each appointment query handler rather than via a background job (no
/// scheduler infrastructure in this app — Modules 9/10 are deferred), so the stored Status stays
/// the single source of truth for filters/UI instead of being recomputed display-only.
/// </summary>
public static class AppointmentAutoStatus
{
    public static bool ShouldAutoNoShow(Appointment appointment, DateOnly today) =>
        appointment.Status == AppointmentStatuses.Scheduled && appointment.AppointmentDate < today;
}
