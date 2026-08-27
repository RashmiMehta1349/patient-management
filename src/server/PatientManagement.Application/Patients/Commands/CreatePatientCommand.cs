using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Common;
using PatientManagement.Application.Patients.Dtos;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Application.Patients.Commands;

/// <summary>
/// Validates and persists a new Patient. Server-side validation is mandatory (defense in
/// depth) regardless of client-side checks, since this is the anchor entity every other
/// module depends on.
/// </summary>
public class CreatePatientCommandHandler
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePatientCommandHandler(IPatientRepository patientRepository, IDateTimeProvider dateTimeProvider)
    {
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PatientDto>> HandleAsync(CreatePatientRequestDto request, CancellationToken cancellationToken = default)
    {
        var errors = PatientValidation.Validate(request.FullName, request.DateOfBirth, request.Gender, request.CountryCode, request.PhoneNumber, _dateTimeProvider.UtcNow, out var dateOfBirth);
        if (errors.Count > 0)
        {
            return Result<PatientDto>.Failure(string.Join(" ", errors));
        }

        var now = _dateTimeProvider.UtcNow;
        var patient = new Patient
        {
            FullName = request.FullName.Trim(),
            DateOfBirth = dateOfBirth,
            Gender = request.Gender.Trim(),
            CountryCode = request.CountryCode.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _patientRepository.AddAsync(patient, cancellationToken);

        return Result<PatientDto>.Success(ToDto(patient, now));
    }

    internal static PatientDto ToDto(Patient patient, DateTime utcNow)
    {
        var today = DateOnly.FromDateTime(utcNow);
        var age = today.Year - patient.DateOfBirth.Year;
        if (patient.DateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        return new PatientDto
        {
            Id = patient.Id,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Age = age,
            Gender = patient.Gender,
            CountryCode = patient.CountryCode,
            PhoneNumber = patient.PhoneNumber,
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt
        };
    }
}
