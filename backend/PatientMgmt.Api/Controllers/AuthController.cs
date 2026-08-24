using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PatientMgmt.BusinessLogic.Auth;
using PatientMgmt.Domain.Contracts;
using System.Security.Claims;

namespace PatientMgmt.Api.Controllers
{
    /// <summary>
    /// Thin controller: model binding, HTTP status mapping, delegates all rules to services.
    /// Base path convention: /api/v1/auth/... established here for reuse by later modules.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private const string GenericLoginError = "Invalid username or password.";
        private const string GenericForgotPasswordMessage = "If that account exists, a reset link has been sent.";

        private readonly IAuthService _authService;
        private readonly IPasswordResetService _passwordResetService;

        public AuthController(IAuthService authService, IPasswordResetService passwordResetService)
        {
            _authService = authService;
            _passwordResetService = passwordResetService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new MessageResponse(GenericLoginError));

            var result = await _authService.LoginAsync(request.UsernameOrEmail, request.Password, ct);
            if (!result.Success)
                return Unauthorized(new MessageResponse(GenericLoginError));

            return Ok(new LoginResponse(result.AccessToken!, result.ExpiresAt!.Value));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var sessionId = GetSessionId();
            if (sessionId is null)
                return Unauthorized();

            await _authService.LogoutAsync(sessionId.Value, ct);
            return NoContent();
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
        {
            // Always identical response shape/timing regardless of match outcome (no enumeration).
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                await _passwordResetService.RequestResetAsync(request.Email, ct);
            }
            return Ok(new MessageResponse(GenericForgotPasswordMessage));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponse>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            var result = await _passwordResetService.CompleteResetAsync(request.Token, request.NewPassword, ct);
            if (!result.Success)
                return BadRequest(new MessageResponse("This reset link is invalid or has expired. Please request a new one."));

            return Ok(new MessageResponse("Your password has been reset. Please log in with your new password."));
        }

        [HttpGet("session")]
        [Authorize]
        public ActionResult<SessionCheckResponse> Session()
        {
            // JwtSessionMiddleware has already validated the session (IsValid + idle timeout)
            // and refreshed LastActivityAt before this action runs; reaching here means "still logged in".
            var email = User.FindFirstValue(System.Security.Claims.ClaimTypes.Email)
                        ?? User.FindFirstValue("email");
            return Ok(new SessionCheckResponse(true, email));
        }

        private Guid? GetSessionId()
        {
            var sidClaim = User.FindFirstValue("sid");
            return Guid.TryParse(sidClaim, out var sid) ? sid : null;
        }
    }
}
