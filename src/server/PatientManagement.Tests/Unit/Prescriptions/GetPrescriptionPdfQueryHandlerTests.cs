using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Prescriptions.Dtos;
using PatientManagement.Application.Prescriptions.Queries;
using PatientManagement.Application.Prescriptions.Services;
using PatientManagement.Application.Visits.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Prescriptions;

public class GetPrescriptionPdfQueryHandlerTests
{
    private readonly Mock<IVisitRepository> _visitRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IPrescriptionPdfGenerator> _pdfGenerator = new();

    private GetPrescriptionPdfQueryHandler CreateHandler() =>
        new(_visitRepository.Object, _patientRepository.Object, _pdfGenerator.Object);

    [Fact]
    public async Task UnknownVisit_ReturnsNull()
    {
        _visitRepository.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Visit?)null);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Random.Shared.NextInt64(1, long.MaxValue));

        Assert.Null(result);
        _pdfGenerator.Verify(g => g.Generate(It.IsAny<PrescriptionDocumentDto>()), Times.Never);
    }

    [Fact]
    public async Task KnownVisit_ComposesDocumentAndReturnsGeneratedBytes()
    {
        var patientId = Random.Shared.NextInt64(1, long.MaxValue);
        var visit = new Visit
        {
            Id = Random.Shared.NextInt64(1, long.MaxValue),
            PatientId = patientId,
            VisitDate = new DateTime(2026, 8, 25),
            Diagnosis = "Flu",
            TemperatureNotRecorded = true,
            BloodPressureNotRecorded = true,
            PulseNotRecorded = true
        };
        _visitRepository.Setup(r => r.GetByIdAsync(visit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(visit);
        _patientRepository
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe", Gender = "Female", PhoneNumber = "555", DateOfBirth = new DateOnly(1990, 1, 1) });
        var expectedBytes = new byte[] { 1, 2, 3 };
        _pdfGenerator
            .Setup(g => g.Generate(It.Is<PrescriptionDocumentDto>(d => d.PatientName == "Jane Doe" && d.Diagnosis == "Flu")))
            .Returns(expectedBytes);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(visit.Id);

        Assert.Equal(expectedBytes, result);
    }

    [Fact]
    public async Task MissingPatientForVisit_ReturnsNull()
    {
        var visit = new Visit { Id = Random.Shared.NextInt64(1, long.MaxValue), PatientId = Random.Shared.NextInt64(1, long.MaxValue), VisitDate = DateTime.UtcNow };
        _visitRepository.Setup(r => r.GetByIdAsync(visit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(visit);
        _patientRepository.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(visit.Id);

        Assert.Null(result);
    }
}
