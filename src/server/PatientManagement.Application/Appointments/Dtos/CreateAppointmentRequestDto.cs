using System;

namespace PatientManagement.Application.Appointments.Dtos;

public class CreateAppointmentRequestDto
{
    public long PatientId { get; set; }

    /// <summary>ISO 8601 date string (yyyy-MM-dd), e.g. from an HTML date input.</summary>
    public string AppointmentDate { get; set; } = string.Empty;

    /// <summary>24-hour "HH:mm" string, e.g. from an HTML time input.</summary>
    public string AppointmentTime { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
