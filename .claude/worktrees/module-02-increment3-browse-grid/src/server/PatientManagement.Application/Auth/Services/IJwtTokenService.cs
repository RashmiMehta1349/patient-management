using System;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Auth.Services;

public interface IJwtTokenService
{
    /// <summary>Issues a signed JWT for the given user, embedding sub/email/security_stamp/exp claims.</summary>
    (string Token, DateTime ExpiresAtUtc) IssueToken(User user);
}
