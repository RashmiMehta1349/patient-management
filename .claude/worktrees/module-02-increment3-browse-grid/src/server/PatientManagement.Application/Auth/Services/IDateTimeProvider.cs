using System;

namespace PatientManagement.Application.Auth.Services;

/// <summary>Injectable clock so time-dependent logic (token expiry) is unit-testable.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
