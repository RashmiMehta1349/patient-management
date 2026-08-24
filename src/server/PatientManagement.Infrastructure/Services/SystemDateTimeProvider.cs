using System;
using PatientManagement.Application.Auth.Services;

namespace PatientManagement.Infrastructure.Services;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
