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

public class SearchPatientsQueryTests
{
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    public SearchPatientsQueryTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private SearchPatientsQueryHandler CreateHandler() => new(_patientRepository.Object, _dateTimeProvider.Object);

    private static Patient MakePatient(string name, string phone = "555-000-0000") => new()
    {
        Id = Guid.NewGuid(),
        FullName = name,
        DateOfBirth = new DateOnly(1990, 1, 1),
        Gender = "Female",
        PhoneNumber = phone,
        CreatedAt = FixedUtcNow,
        UpdatedAt = FixedUtcNow
    };

    [Fact]
    public async Task QueryMatchingMoreRowsThanOnePage_ReturnsOnlyCurrentPageMatches()
    {
        var pageItems = Enumerable.Range(1, 5).Select(i => MakePatient($"Jane {i}")).ToList();
        _patientRepository
            .Setup(r => r.SearchAsync("Jane", 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Patient>)pageItems, 12));

        var result = await CreateHandler().HandleAsync("Jane", 1, 5);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(12, result.TotalCount);
    }

    [Fact]
    public async Task PageSizeAboveMax_IsClampedTo100()
    {
        _patientRepository
            .Setup(r => r.SearchAsync("Jane", 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Patient>)new List<Patient>(), 0));

        var result = await CreateHandler().HandleAsync("Jane", 1, 500);

        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task ZeroOrNegativePage_FallsBackToPage1()
    {
        _patientRepository
            .Setup(r => r.SearchAsync("Jane", 1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Patient>)new List<Patient>(), 0));

        var result = await CreateHandler().HandleAsync("Jane", -1, 25);

        Assert.Equal(1, result.Page);
    }
}
