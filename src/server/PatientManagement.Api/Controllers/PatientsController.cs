using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Patients.Commands;
using PatientManagement.Application.Patients.Dtos;

namespace PatientManagement.Api.Controllers;

/// <summary>
/// All endpoints here rely on the app's fallback authorization policy (RequireAuthenticatedUser)
/// configured in Program.cs — no [AllowAnonymous] is added for this module.
/// </summary>
[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly CreatePatientCommandHandler _createPatientHandler;

    public PatientsController(CreatePatientCommandHandler createPatientHandler)
    {
        _createPatientHandler = createPatientHandler;
    }

    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create([FromBody] CreatePatientRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _createPatientHandler.HandleAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        // No GET /api/patients/{id} endpoint exists yet (that lands in Increment 2), so the
        // Location header is built manually rather than via CreatedAtAction/CreatedAtRoute.
        return Created($"/api/patients/{result.Value!.Id}", result.Value);
    }
}
