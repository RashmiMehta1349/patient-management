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
    private readonly GetAllPatientsQueryHandler _getAllPatientsHandler;
    private readonly SearchPatientsQueryHandler _searchPatientsHandler;

    public PatientsController(
        CreatePatientCommandHandler createPatientHandler,
        GetPatientByIdQueryHandler getPatientByIdHandler,
        UpdatePatientCommandHandler updatePatientHandler,
        GetAllPatientsQueryHandler getAllPatientsHandler,
        SearchPatientsQueryHandler searchPatientsHandler)
    {
        _createPatientHandler = createPatientHandler;
        _getPatientByIdHandler = getPatientByIdHandler;
        _updatePatientHandler = updatePatientHandler;
        _getAllPatientsHandler = getAllPatientsHandler;
        _searchPatientsHandler = searchPatientsHandler;
    }

    /// <summary>
    /// Serves both the browse-all and search cases, distinguished only by whether `query` is
    /// present/non-empty (Increment 3 revision, §9b.1). Both branches are paginated using the
    /// same page/pageSize params and response envelope.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<PatientDto>>> GetAll(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = string.IsNullOrWhiteSpace(query)
            ? await _getAllPatientsHandler.HandleAsync(page, pageSize, cancellationToken)
            : await _searchPatientsHandler.HandleAsync(query, page, pageSize, cancellationToken);

        return Ok(result);
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

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PatientDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var patient = await _getPatientByIdHandler.HandleAsync(id, cancellationToken);
        if (patient is null)
        {
            return NotFound(new { message = "Patient not found." });
        }

        return Ok(patient);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<PatientDto>> Update(long id, [FromBody] UpdatePatientRequestDto request, CancellationToken cancellationToken)
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
