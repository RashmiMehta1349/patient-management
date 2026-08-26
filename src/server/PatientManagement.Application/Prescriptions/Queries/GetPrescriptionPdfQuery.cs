using System;
using System.Threading;
using System.Threading.Tasks;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Prescriptions.Dtos;
using PatientManagement.Application.Prescriptions.Services;
using PatientManagement.Application.Visits;
using PatientManagement.Application.Visits.Services;

namespace PatientManagement.Application.Prescriptions.Queries;

/// <summary>
/// Composes a visit + its patient into a printable prescription PDF (read-only — no data written,
/// R7). Not-found (unknown visit) is a normal expected outcome, mirroring GetVisitByIdQueryHandler's
/// null-return convention.
/// </summary>
public class GetPrescriptionPdfQueryHandler
{
    private readonly IVisitRepository _visitRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IPrescriptionPdfGenerator _pdfGenerator;

    public GetPrescriptionPdfQueryHandler(
        IVisitRepository visitRepository,
        IPatientRepository patientRepository,
        IPrescriptionPdfGenerator pdfGenerator)
    {
        _visitRepository = visitRepository;
        _patientRepository = patientRepository;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<byte[]?> HandleAsync(long visitId, CancellationToken cancellationToken = default)
    {
        var visit = await _visitRepository.GetByIdAsync(visitId, cancellationToken);
        if (visit is null)
        {
            return null;
        }

        var patient = await _patientRepository.GetByIdAsync(visit.PatientId, cancellationToken);
        if (patient is null)
        {
            return null;
        }

        var age = CalculateAge(patient.DateOfBirth);

        var document = new PrescriptionDocumentDto
        {
            PatientName = patient.FullName,
            PatientGender = patient.Gender,
            PatientAge = age,
            PatientPhoneNumber = patient.PhoneNumber,
            VisitDate = visit.VisitDate,
            TemperatureValue = visit.TemperatureValue,
            TemperatureNotRecorded = visit.TemperatureNotRecorded,
            BloodPressureValue = visit.BloodPressureValue,
            BloodPressureNotRecorded = visit.BloodPressureNotRecorded,
            PulseValue = visit.PulseValue,
            PulseNotRecorded = visit.PulseNotRecorded,
            Diagnosis = visit.Diagnosis,
            Medications = VisitMapper.ToDto(visit, patient.FullName).Medications
        };

        return _pdfGenerator.Generate(document);
    }

    private static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}
