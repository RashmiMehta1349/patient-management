using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Auth.Commands;
using PatientManagement.Application.Auth.Dtos;
using PatientManagement.Application.Auth.Services;

namespace PatientManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly LoginCommandHandler _loginHandler;
    private readonly IUserRepository _userRepository;

    public AuthController(
        LoginCommandHandler loginHandler,
        IUserRepository userRepository)
    {
        _loginHandler = loginHandler;
        _userRepository = userRepository;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _loginHandler.HandleAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdClaim is null || !long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email,
            LastLoginAt = user.LastLoginAt
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        // Stateless JWT: logout is a client-side token discard. No server-side session to
        // invalidate — no concurrent-session handling is required per BRD scope.
        return Ok(new { message = "Logged out." });
    }
}
