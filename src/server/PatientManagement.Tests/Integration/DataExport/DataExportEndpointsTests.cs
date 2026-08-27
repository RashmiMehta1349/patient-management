using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PatientManagement.Tests.Integration.Auth;
using UglyToad.PdfPig;
using Xunit;

namespace PatientManagement.Tests.Integration.DataExport;

/// <summary>
/// Module 8 (Data Export) — plan §13. Reuses AuthWebApplicationFactory (isolated in-memory SQLite
/// DB seeded with a known test user) since DataExportController sits behind the same JWT-protected
/// fallback policy as every other controller.
/// </summary>
public class DataExportEndpointsTests : IDisposable
{
    private readonly AuthWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("/api/visits/{0}/export/csv")]
    [InlineData("/api/visits/{0}/export/pdf")]
    [InlineData("/api/patients/{0}/export/csv")]
    [InlineData("/api/patients/{0}/export/pdf")]
    public async Task ExportEndpoints_WithoutToken_ReturnUnauthorized(string routeTemplate)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(string.Format(routeTemplate, Random.Shared.NextInt64(1, long.MaxValue)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/visits/{0}/export/csv")]
    [InlineData("/api/visits/{0}/export/pdf")]
    public async Task VisitExport_ForUnknownVisit_ReturnsNotFound(string routeTemplate)
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync(string.Format(routeTemplate, Random.Shared.NextInt64(1, long.MaxValue)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/patients/{0}/export/csv")]
    [InlineData("/api/patients/{0}/export/pdf")]
    public async Task PatientExport_ForUnknownPatient_ReturnsNotFound(string routeTemplate)
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync(string.Format(routeTemplate, Random.Shared.NextInt64(1, long.MaxValue)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExportVisitCsv_ForSeededVisit_ReturnsCsvWithComplaintsAndCorrectContentDisposition()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client, "Alice Example");
        var visitId = await CreateVisitAsync(client, patientId, complaints: "Cough, fever\nand chills", diagnosis: "Migraine");

        var response = await client.GetAsync($"/api/visits/{visitId}/export/csv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal($"visit-{visitId}-export.csv", response.Content.Headers.ContentDisposition?.FileName);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alice Example", csv);
        Assert.Contains("Migraine", csv);
        // Structural round-trip: the embedded comma/newline field must be quoted, not silently
        // split across unrelated columns.
        Assert.Contains("\"Cough, fever\nand chills\"", csv);
    }

    [Fact]
    public async Task ExportVisitPdf_ForSeededVisit_ReturnsValidPdfIncludingComplaints()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client, "Bob Complaints");
        var visitId = await CreateVisitAsync(client, patientId, complaints: "Sore throat", diagnosis: "Pharyngitis");

        var response = await client.GetAsync($"/api/visits/{visitId}/export/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));
        Assert.Contains("Sore throat", text);
        Assert.Contains("Pharyngitis", text);
    }

    [Fact]
    public async Task ExportPatientCsv_WithoutIncludeHistory_OmitsHistorySection()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client, "Carol NoHistory");
        await CreateVisitAsync(client, patientId, complaints: "Headache", diagnosis: "Tension headache");

        var response = await client.GetAsync($"/api/patients/{patientId}/export/csv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Carol NoHistory", csv);
        Assert.DoesNotContain("Visit History", csv);
    }

    [Fact]
    public async Task ExportPatientCsv_WithIncludeHistoryTrue_IncludesSummarizedHistory()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client, "Dave History");
        await CreateVisitAsync(client, patientId, complaints: "Back pain", diagnosis: "Muscle strain");

        var response = await client.GetAsync($"/api/patients/{patientId}/export/csv?includeHistory=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Dave History", csv);
        Assert.Contains("Visit History", csv);
        Assert.Contains("Muscle strain", csv);
    }

    [Fact]
    public async Task ExportPatientPdf_WithIncludeHistoryTrue_ZeroVisits_ReturnsEmptyHistoryNotError()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client, "Erin ZeroVisits");

        var response = await client.GetAsync($"/api/patients/{patientId}/export/pdf?includeHistory=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));
        Assert.Contains("Erin ZeroVisits", text);
        Assert.Contains("No visits recorded.", text);
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<long> CreatePatientAsync(HttpClient client, string fullName)
    {
        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            fullName,
            dateOfBirth = "1990-05-15",
            gender = "Female",
            countryCode = "+91",
            phoneNumber = "9876543210"
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return long.Parse(body.GetProperty("id").GetString()!);
    }

    private static async Task<long> CreateVisitAsync(HttpClient client, long patientId, string complaints, string diagnosis)
    {
        var response = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true,
            complaints,
            diagnosis
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return long.Parse(body.GetProperty("id").GetString()!);
    }

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = AuthWebApplicationFactory.TestUserEmail,
            password = AuthWebApplicationFactory.TestUserPassword
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }
}
