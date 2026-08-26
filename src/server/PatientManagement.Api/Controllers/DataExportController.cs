using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.DataExport.Queries;
using PatientManagement.Application.DataExport.Services;

namespace PatientManagement.Api.Controllers;

/// <summary>
/// Module 8 (Data Export) — plan §5 Open Question 2, resolved to a single, dedicated controller
/// spanning both anchor resources (patient and visit) rather than extending PatientsController/
/// VisitsController, since export reads as one cohesive "give me this record as a file" capability.
/// Routes are nested under each resource for discoverability
/// (/api/patients/{id}/export/*, /api/visits/{id}/export/*). All endpoints rely on the app's
/// fallback authorization policy (RequireAuthenticatedUser) configured in Program.cs — no
/// [AllowAnonymous] added, consistent with every other controller in this codebase. Every export
/// query is parameterized by exactly one patientId or visitId (R7) — no join/aggregation beyond
/// that single record's own data.
/// </summary>
[ApiController]
[Route("api")]
public class DataExportController : ControllerBase
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly GetPatientExportQueryHandler _getPatientExportHandler;
    private readonly GetVisitExportQueryHandler _getVisitExportHandler;
    private readonly IPatientExportPdfGenerator _patientExportPdfGenerator;
    private readonly IVisitExportPdfGenerator _visitExportPdfGenerator;
    private readonly ICsvWriter _csvWriter;

    public DataExportController(
        GetPatientExportQueryHandler getPatientExportHandler,
        GetVisitExportQueryHandler getVisitExportHandler,
        IPatientExportPdfGenerator patientExportPdfGenerator,
        IVisitExportPdfGenerator visitExportPdfGenerator,
        ICsvWriter csvWriter)
    {
        _getPatientExportHandler = getPatientExportHandler;
        _getVisitExportHandler = getVisitExportHandler;
        _patientExportPdfGenerator = patientExportPdfGenerator;
        _visitExportPdfGenerator = visitExportPdfGenerator;
        _csvWriter = csvWriter;
    }

    [HttpGet("patients/{id:long}/export/csv")]
    public async Task<ActionResult> ExportPatientCsv(long id, [FromQuery] bool includeHistory, CancellationToken cancellationToken)
    {
        var document = await _getPatientExportHandler.HandleAsync(id, includeHistory, cancellationToken);
        if (document is null)
        {
            return NotFound(new { message = "Patient not found." });
        }

        var csv = _csvWriter.WritePatientExport(document);
        return File(Utf8NoBom.GetBytes(csv), "text/csv", $"patient-{id}-export.csv");
    }

    [HttpGet("patients/{id:long}/export/pdf")]
    public async Task<ActionResult> ExportPatientPdf(long id, [FromQuery] bool includeHistory, CancellationToken cancellationToken)
    {
        var document = await _getPatientExportHandler.HandleAsync(id, includeHistory, cancellationToken);
        if (document is null)
        {
            return NotFound(new { message = "Patient not found." });
        }

        var pdfBytes = _patientExportPdfGenerator.Generate(document);
        return File(pdfBytes, "application/pdf", $"patient-{id}-export.pdf");
    }

    [HttpGet("visits/{id:long}/export/csv")]
    public async Task<ActionResult> ExportVisitCsv(long id, CancellationToken cancellationToken)
    {
        var document = await _getVisitExportHandler.HandleAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound(new { message = "Visit not found." });
        }

        var csv = _csvWriter.WriteVisitExport(document);
        return File(Utf8NoBom.GetBytes(csv), "text/csv", $"visit-{id}-export.csv");
    }

    [HttpGet("visits/{id:long}/export/pdf")]
    public async Task<ActionResult> ExportVisitPdf(long id, CancellationToken cancellationToken)
    {
        var document = await _getVisitExportHandler.HandleAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound(new { message = "Visit not found." });
        }

        var pdfBytes = _visitExportPdfGenerator.Generate(document);
        return File(pdfBytes, "application/pdf", $"visit-{id}-export.pdf");
    }
}
