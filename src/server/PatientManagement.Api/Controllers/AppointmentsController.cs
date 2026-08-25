using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Appointments.Commands;
using PatientManagement.Application.Appointments.Dtos;
using PatientManagement.Application.Appointments.Queries;

namespace PatientManagement.Api.Controllers;

/// <summary>
/// All endpoints here rely on the app's fallback authorization policy (RequireAuthenticatedUser)
/// configured in Program.cs — no [AllowAnonymous] is added for this module.
/// </summary>
[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly CreateAppointmentCommandHandler _createAppointmentHandler;
    private readonly GetAppointmentsByDateQueryHandler _getAppointmentsByDateHandler;
    private readonly GetAppointmentByIdQueryHandler _getAppointmentByIdHandler;
    private readonly UpdateAppointmentStatusCommandHandler _updateAppointmentStatusHandler;
    private readonly UpdateAppointmentCommandHandler _updateAppointmentHandler;
    private readonly GetAppointmentsByPatientIdQueryHandler _getAppointmentsByPatientIdHandler;

    public AppointmentsController(
        CreateAppointmentCommandHandler createAppointmentHandler,
        GetAppointmentsByDateQueryHandler getAppointmentsByDateHandler,
        GetAppointmentByIdQueryHandler getAppointmentByIdHandler,
        UpdateAppointmentStatusCommandHandler updateAppointmentStatusHandler,
        UpdateAppointmentCommandHandler updateAppointmentHandler,
        GetAppointmentsByPatientIdQueryHandler getAppointmentsByPatientIdHandler)
    {
        _createAppointmentHandler = createAppointmentHandler;
        _getAppointmentsByDateHandler = getAppointmentsByDateHandler;
        _getAppointmentByIdHandler = getAppointmentByIdHandler;
        _updateAppointmentStatusHandler = updateAppointmentStatusHandler;
        _updateAppointmentHandler = updateAppointmentHandler;
        _getAppointmentsByPatientIdHandler = getAppointmentsByPatientIdHandler;
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create([FromBody] CreateAppointmentRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _createAppointmentHandler.HandleAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Serves the daily list (?date=) and the patient-scoped list (?patientId=) — mutually
    /// exclusive filters on the same GET route, mirroring how GET /api/patients branches on
    /// `query` presence (approved plan §6).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] string? date, [FromQuery] Guid? patientId, CancellationToken cancellationToken)
    {
        if (patientId.HasValue)
        {
            var byPatient = await _getAppointmentsByPatientIdHandler.HandleAsync(patientId.Value, cancellationToken);
            return Ok(byPatient);
        }

        if (string.IsNullOrWhiteSpace(date) || !DateOnly.TryParse(date, out var parsedDate))
        {
            return BadRequest(new { message = "A valid date (yyyy-MM-dd) or patientId is required." });
        }

        var byDate = await _getAppointmentsByDateHandler.HandleAsync(parsedDate, cancellationToken);
        return Ok(byDate);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await _getAppointmentByIdHandler.HandleAsync(id, cancellationToken);
        if (appointment is null)
        {
            return NotFound(new { message = "Appointment not found." });
        }

        return Ok(appointment);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<AppointmentDto>> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatusRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _updateAppointmentStatusHandler.HandleAsync(id, request, cancellationToken);
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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> Update(Guid id, [FromBody] UpdateAppointmentRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _updateAppointmentHandler.HandleAsync(id, request, cancellationToken);
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
