using System.Security.Claims;
using PatientMgmt.BusinessLogic.Auth;

namespace PatientMgmt.Api.Middleware
{
    /// <summary>
    /// Runs after JWT signature/expiry validation (UseAuthentication). For every request
    /// carrying an authenticated JWT, looks up the "sid" claim against the Sessions table
    /// and enforces idle-timeout + IsValid/invalidation (e.g. post password-reset), refreshing
    /// LastActivityAt on success. This is the shared session-revocation gate every protected
    /// endpoint in every module rides on top of the base JwtBearer handler.
    /// </summary>
    public class JwtSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtSessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var sidClaim = context.User.FindFirstValue("sid");
                if (!Guid.TryParse(sidClaim, out var sessionId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                var validation = await authService.ValidateSessionAsync(sessionId, context.RequestAborted);
                if (!validation.IsValid)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            await _next(context);
        }
    }

    public static class JwtSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtSessionValidation(this IApplicationBuilder app)
        {
            return app.UseMiddleware<JwtSessionMiddleware>();
        }
    }
}
