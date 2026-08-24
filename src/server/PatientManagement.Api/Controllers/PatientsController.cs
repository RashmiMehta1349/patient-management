using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Patients.Commands;
using PatientManagement.Application.Patients.Dtos;
using PatientManagement.Application.Patients.Queries;

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
    private readonly GetPatientByIdQueryHandler _getPatientByIdHandler;
    private readonly UpdatePatientCommandHandler _updatePatientHandler;

    public PatientsController(
        CreatePatientCommandHandler createPatientHandler,
        GetPatientByIdQueryHandler getPatientByIdHandler,
        UpdatePatientCommandHandler updatePatientHandler)
    {
        _createPatientHandler = createPatientHandler;
        _getPatientByIdHandler = getPatientByIdHandler;
        _updatePatientHandler = updatePatientHandler;
    }

    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create([FromBody] CreatePatientRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _createPatientHandler.HandleAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _getPatientByIdHandler.HandleAsync(id, cancellationToken);
        if (patient is null)
        {
            return NotFound(new { message = "Patient not found." });
        }

        return Ok(patient);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientDto>> Update(Guid id, [FromBody] UpdatePatientRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _updatePatientHandler.HandleAsync(id, request, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound(new { message = result.Error });
        }

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }
}
