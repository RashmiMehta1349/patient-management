using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using PatientManagement.Tests.Integration.Auth;
using UglyToad.PdfPig;
using Xunit;

namespace PatientManagement.Tests.Integration.Prescriptions;

/// <summary>
/// Module 5 — GET /api/visits/{id}/prescription/pdf, the server-generated PDF endpoint (product
/// decision overriding the plan's original browser-print recommendation).
/// </summary>
public class PrescriptionPdfEndpointTests : IDisposable
{
    private readonly AuthWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetPrescriptionPdf_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/visits/{Guid.NewGuid()}/prescription/pdf");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPrescriptionPdf_ForUnknownVisit_ReturnsNotFound()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/visits/{Guid.NewGuid()}/prescription/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPrescriptionPdf_ForVisitWithMedications_ReturnsValidPdfWithExpectedContent()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client, "Alice Example");
        var createResponse = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true,
            diagnosis = "Migraine",
            medications = new[]
            {
                new { name = "Ibuprofen", dosage = "200mg", frequency = "Twice daily", duration = "3 days", instructions = "After food" }
            }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var visitId = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/visits/{visitId}/prescription/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));
        Assert.Contains("Alice Example", text);
        Assert.Contains("Migraine", text);
        Assert.Contains("Ibuprofen", text);
    }

    [Fact]
    public async Task GetPrescriptionPdf_ForVisitWithNoMedications_ReturnsValidPdfWithEmptyState()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client, "Bob NoMeds");
        var createResponse = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var visitId = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/visits/{visitId}/prescription/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));
        Assert.Contains("No medications prescribed", text);
        Assert.Contains("Bob NoMeds", text);
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<Guid> CreatePatientAsync(HttpClient client, string fullName)
    {
        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            fullName,
            dateOfBirth = "1990-05-15",
            gender = "Female",
            phoneNumber = "555-123-4567"
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("id").GetString()!);
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
