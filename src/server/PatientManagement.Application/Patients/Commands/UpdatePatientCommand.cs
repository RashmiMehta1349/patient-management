using System;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Common;
using PatientManagement.Application.Patients.Dtos;
using PatientManagement.Application.Patients.Services;

namespace PatientManagement.Application.Patients.Commands;

/// <summary>
/// Validates and persists an edit to an existing Patient (full-payload PUT). Reuses the shared
/// PatientValidation helper so create/edit validation can't drift apart, and
/// CreatePatientCommandHandler.ToDto so the response shape stays identical.
/// </summary>
public class UpdatePatientCommandHandler
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdatePatientCommandHandler(IPatientRepository patientRepository, IDateTimeProvider dateTimeProvider)
    {
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PatientDto>> HandleAsync(Guid id, UpdatePatientRequestDto request, CancellationToken cancellationToken = default)
    {
        var patient = await _patientRepository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return Result<PatientDto>.NotFound("Patient not found.");
        }

        var now = _dateTimeProvider.UtcNow;
        var errors = PatientValidation.Validate(request.FullName, request.DateOfBirth, request.Gender, request.PhoneNumber, now, out var dateOfBirth);
        if (errors.Count > 0)
        {
            return Result<PatientDto>.Failure(string.Join(" ", errors));
        }

        patient.FullName = request.FullName.Trim();
        patient.DateOfBirth = dateOfBirth;
        patient.Gender = request.Gender.Trim();
        patient.PhoneNumber = request.PhoneNumber.Trim();
        patient.UpdatedAt = now;

        await _patientRepository.UpdateAsync(patient, cancellationToken);

        return Result<PatientDto>.Success(CreatePatientCommandHandler.ToDto(patient, now));
    }
}
