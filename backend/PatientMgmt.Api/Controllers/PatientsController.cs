using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientMgmt.BusinessLogic.Interfaces;
using PatientMgmt.Domain.Contracts;

namespace PatientMgmt.Api.Controllers
{
    /// <summary>
    /// Thin controller: model binding, HTTP status mapping, delegates all rules to
    /// PatientService. Base path convention: /api/v1/patients/... (Module 2 plan §6).
    /// Every endpoint requires an authenticated session (JwtSessionMiddleware) — no anonymous
    /// access to patient data anywhere in this controller.
    /// </summary>
    [ApiController]
    [Route("api/v1/patients")]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        [HttpPost]
        public async Task<ActionResult<PatientResponse>> Create([FromBody] CreatePatientRequest request, CancellationToken ct)
        {
            var result = await _patientService.CreateAsync(
                request.FullName, request.DateOfBirth, request.ApproxAgeAtEntry, request.Gender,
                request.PhoneNumber, request.Email, request.Address, ct);

            if (!result.Success)
                return BadRequest(ToValidationErrorResponse(result.Errors));

            var response = PatientResponse.FromEntity(result.Patient!);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PatientResponse>> GetById(Guid id, CancellationToken ct)
        {
            var patient = await _patientService.GetByIdAsync(id, ct);
            if (patient is null)
                return NotFound();

            return Ok(PatientResponse.FromEntity(patient));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<PatientResponse>> Update(Guid id, [FromBody] UpdatePatientRequest request, CancellationToken ct)
        {
            var result = await _patientService.UpdateAsync(
                id, request.FullName, request.DateOfBirth, request.ApproxAgeAtEntry, request.Gender,
                request.PhoneNumber, request.Email, request.Address, ct);

            if (!result.Success)
            {
                if (result.Errors.Any(e => e.Field == "id"))
                    return NotFound();
                return BadRequest(ToValidationErrorResponse(result.Errors));
            }

            return Ok(PatientResponse.FromEntity(result.Patient!));
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<PatientResponse>>> Search(
            [FromQuery] string? search, [FromQuery] string? sort, [FromQuery] int? limit, CancellationToken ct)
        {
            // Recent-patients hook for Module 7 (Search & Navigation) navigation consumption (task #9).
            if (string.Equals(sort, "recent", StringComparison.OrdinalIgnoreCase))
            {
                var recentPatients = await _patientService.GetRecentAsync(limit is > 0 ? limit.Value : 10, ct);
                return Ok(recentPatients.Select(PatientResponse.FromEntity).ToList());
            }

            if (string.IsNullOrWhiteSpace(search))
                return Ok(Array.Empty<PatientResponse>());

            var patients = await _patientService.SearchAsync(search, ct);
            return Ok(patients.Select(PatientResponse.FromEntity).ToList());
        }

        [HttpGet("check-duplicate")]
        public async Task<ActionResult<DuplicateCheckResponse>> CheckDuplicate([FromQuery] string? name, [FromQuery] string? phone, CancellationToken ct)
        {
            var result = await _patientService.CheckDuplicateAsync(name ?? string.Empty, phone ?? string.Empty, ct);
            return Ok(new DuplicateCheckResponse(
                result.PossibleDuplicate,
                result.ExistingPatient?.Id,
                result.ExistingPatient?.PatientCode));
        }

        private static ValidationErrorResponse ToValidationErrorResponse(IReadOnlyList<BusinessLogic.Patients.FieldValidationError> errors) =>
            new("Validation failed.", errors.Select(e => new FieldError(e.Field, e.Message)).ToList());
    }
}
