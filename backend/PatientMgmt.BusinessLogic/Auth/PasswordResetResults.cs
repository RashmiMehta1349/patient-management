namespace PatientMgmt.BusinessLogic.Auth
{
    public enum ResetCompletionStatus
    {
        Success,
        InvalidOrExpiredToken
    }

    public class ResetCompletionResult
    {
        public ResetCompletionStatus Status { get; init; }
        public bool Success => Status == ResetCompletionStatus.Success;

        public static ResetCompletionResult Ok() => new() { Status = ResetCompletionStatus.Success };
        public static ResetCompletionResult Invalid() => new() { Status = ResetCompletionStatus.InvalidOrExpiredToken };
    }
}
