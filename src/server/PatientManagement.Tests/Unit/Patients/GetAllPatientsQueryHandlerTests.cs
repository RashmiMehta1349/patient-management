using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Queries;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Domain.Entities;
using Xunit;

namespace PatientManagement.Tests.Unit.Patients;

public class GetAllPatientsQueryHandlerTests
{
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    public GetAllPatientsQueryHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private GetAllPatientsQueryHandler CreateHandler() => new(_patientRepository.Object, _dateTimeProvider.Object);

    private static Patient MakePatient(string name) => new()
    {
        Id = Random.Shared.NextInt64(1, long.MaxValue),
        FullName = name,
        DateOfBirth = new DateOnly(1990, 1, 1),
        Gender = "Female",
        PhoneNumber = "555-000-0000",
        CreatedAt = FixedUtcNow,
        UpdatedAt = FixedUtcNow
    };

    [Fact]
    public async Task Page1_Of30Patients_ReturnsFirst25AndTotalCount()
    {
        var page1Items = Enumerable.Range(1, 25).Select(i => MakePatient($"Patient {i:D2}")).ToList();
        _patientRepository
            .Setup(r => r.GetAllAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Patient>)page1Items, 30));

        var result = await CreateHandler().HandleAsync(1, 25);

        Assert.Equal(25, result.Items.Count);
        Assert.Equal(30, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
    }

    [Fact]
    public async Task Page2_Of30Patients_ReturnsRemaining5()
    {
        var page2Items = Enumerable.Range(26, 5).Select(i => MakePatient($"Patient {i:D2}")).ToList();
        _patientRepository
            .Setup(r => r.GetAllAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Patient>)page2Items, 30));

        var result = await CreateHandler().HandleAsync(2, 25);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(30, result.TotalCount);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task PageBeyondTotal_ReturnsEmptyItemsButTotalCountUnchanged()
    {
        _patientRepository
            .Setup(r => r.GetAllAsync(3, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Patient>)new List<Patient>(), 30));

        var result = await CreateHandler().HandleAsync(3, 25);

        Assert.Empty(result.Items);
        Assert.Equal(30, result.TotalCount);
    }

    [Fact]
    public async Task NoPatients_ReturnsEmptyItemsAndZeroTotalCount()
    {
        _patientRepository
            .Setup(r => r.GetAllAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Patient>)new List<Patient>(), 0));

        var result = await CreateHandler().HandleAsync(1, 25);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task PageSizeAboveMax_IsClampedTo100()
    {
        _patientRepository
            .Setup(r => r.GetAllAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Patient>)new List<Patient>(), 0));

        var result = await CreateHandler().HandleAsync(1, 500);

        Assert.Equal(100, result.PageSize);
        _patientRepository.Verify(r => r.GetAllAsync(1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ZeroOrNegativePage_FallsBackToPage1()
    {
        _patientRepository
            .Setup(r => r.GetAllAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Patient>)new List<Patient>(), 0));

        var result = await CreateHandler().HandleAsync(0, 25);

        Assert.Equal(1, result.Page);
        _patientRepository.Verify(r => r.GetAllAsync(1, 25, It.IsAny<CancellationToken>()), Times.Once);
    }
}
