using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Prescriptions.Queries;
using PatientManagement.Application.Visits.Commands;
using PatientManagement.Application.Visits.Dtos;
using PatientManagement.Application.Visits.Queries;

namespace PatientManagement.Api.Controllers;

/// <summary>
/// All endpoints here rely on the app's fallback authorization policy (RequireAuthenticatedUser)
/// configured in Program.cs — no [AllowAnonymous] is added for this module.
/// </summary>
[ApiController]
[Route("api/visits")]
public class VisitsController : ControllerBase
{
    private readonly CreateVisitCommandHandler _createVisitHandler;
    private readonly UpdateVisitCommandHandler _updateVisitHandler;
    private readonly GetVisitByIdQueryHandler _getVisitByIdHandler;
    private readonly GetVisitsByPatientIdQueryHandler _getVisitsByPatientIdHandler;
    private readonly GetPrescriptionPdfQueryHandler _getPrescriptionPdfHandler;

    public VisitsController(
        CreateVisitCommandHandler createVisitHandler,
        UpdateVisitCommandHandler updateVisitHandler,
        GetVisitByIdQueryHandler getVisitByIdHandler,
        GetVisitsByPatientIdQueryHandler getVisitsByPatientIdHandler,
        GetPrescriptionPdfQueryHandler getPrescriptionPdfHandler)
    {
        _createVisitHandler = createVisitHandler;
        _updateVisitHandler = updateVisitHandler;
        _getVisitByIdHandler = getVisitByIdHandler;
        _getVisitsByPatientIdHandler = getVisitsByPatientIdHandler;
        _getPrescriptionPdfHandler = getPrescriptionPdfHandler;
    }

    [HttpPost]
    public async Task<ActionResult<VisitDto>> Create([FromBody] CreateVisitRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _createVisitHandler.HandleAsync(request, cancellationToken);
        if (result.IsNotFound)
        {
            return BadRequest(new { message = result.Error });
        }

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Module 6 (Patient History): fromDate/toDate are optional, inclusive, date-only filters on
    /// VisitDate. ASP.NET Core model binding rejects an unparseable date string before this action
    /// even runs (DateTime? binding failure -> automatic 400 via [ApiController]).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] Guid? patientId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken cancellationToken)
    {
        if (!patientId.HasValue)
        {
            return BadRequest(new { message = "patientId is required." });
        }

        var result = await _getVisitsByPatientIdHandler.HandleAsync(patientId.Value, fromDate, toDate, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VisitDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var visit = await _getVisitByIdHandler.HandleAsync(id, cancellationToken);
        if (visit is null)
        {
            return NotFound(new { message = "Visit not found." });
        }

        return Ok(visit);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VisitDto>> Update(Guid id, [FromBody] UpdateVisitRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _updateVisitHandler.HandleAsync(id, request, cancellationToken);
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

    /// <summary>
    /// Module 5 — server-generated PDF prescription (product decision, overrides the approved
    /// plan's original browser-native window.print() recommendation). Read-only: composes the
    /// visit + its patient into a PDF; writes nothing (R7).
    /// </summary>
    [HttpGet("{id:guid}/prescription/pdf")]
    public async Task<ActionResult> GetPrescriptionPdf(Guid id, CancellationToken cancellationToken)
    {
        var pdfBytes = await _getPrescriptionPdfHandler.HandleAsync(id, cancellationToken);
        if (pdfBytes is null)
        {
            return NotFound(new { message = "Visit not found." });
        }

        return File(pdfBytes, "application/pdf", $"prescription-{id}.pdf");
    }
}
