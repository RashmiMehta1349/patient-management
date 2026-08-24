namespace PatientMgmt.BusinessLogic.Auth
{
    /// <summary>Thin abstraction over UTC "now" so time-dependent logic (idle timeout, token expiry) is unit-testable.</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
