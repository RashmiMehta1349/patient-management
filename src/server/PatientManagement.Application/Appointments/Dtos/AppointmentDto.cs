using System;
using System.Collections.Generic;

namespace PatientManagement.Application.Appointments.Dtos;

public class AppointmentDto
{
    public long Id { get; set; }

    public long PatientId { get; set; }

    /// <summary>Hydrated from Patient.FullName for display — not persisted on Appointment
    /// (approved plan §11, no denormalized PII duplication beyond this read-time join).</summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>ISO 8601 date string (yyyy-MM-dd).</summary>
    public string AppointmentDate { get; set; } = string.Empty;

    /// <summary>24-hour "HH:mm" string.</summary>
    public string AppointmentTime { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Always false for Create/Update responses now that overlaps are blocked before
    /// save rather than warned-and-saved (see CreateAppointmentCommandHandler/
    /// UpdateAppointmentCommandHandler). Retained on the DTO for backward compatibility with
    /// list/get-by-id reads, which never compute it either (see
    /// AppointmentMapper.ToDtoWithoutOverlapCheck).</summary>
    public bool HasOverlapWarning { get; set; }

    public List<ConflictingAppointmentDto> ConflictingAppointments { get; set; } = new();
}

public class ConflictingAppointmentDto
{
    public long Id { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string AppointmentTime { get; set; } = string.Empty;
}
