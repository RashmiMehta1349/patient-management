namespace PatientMgmt.BusinessLogic.Auth
{
    public interface IJwtTokenService
    {
        /// <summary>Issues a short-lived JWT carrying the session ID ("sid"), user ID ("sub"), and email claims.</summary>
        string IssueToken(Guid userId, string email, Guid sessionId, DateTime expiresAtUtc);
    }
}
