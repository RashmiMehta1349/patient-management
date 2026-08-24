using PatientMgmt.BusinessLogic.Auth;

namespace PatientMgmt.BusinessLogic.Tests.Auth
{
    public class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }
}
