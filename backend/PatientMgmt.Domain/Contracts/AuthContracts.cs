namespace PatientMgmt.Domain.Contracts
{
    public record LoginRequest(string UsernameOrEmail, string Password);

    public record LoginResponse(string AccessToken, DateTime ExpiresAt);

    public record ForgotPasswordRequest(string Email);

    public record ResetPasswordRequest(string Token, string NewPassword);

    public record SessionCheckResponse(bool Authenticated, string? Email);

    /// <summary>
    /// Uniform, generic message shape used for both failure and non-committal
    /// success responses (login failure, forgot-password) to avoid enumeration/hinting.
    /// </summary>
    public record MessageResponse(string Message);
}
