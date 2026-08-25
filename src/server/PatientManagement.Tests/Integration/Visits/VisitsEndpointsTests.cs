using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using PatientManagement.Tests.Integration.Auth;
using Xunit;

namespace PatientManagement.Tests.Integration.Visits;

/// <summary>
/// Reuses AuthWebApplicationFactory (isolated in-memory SQLite DB seeded with a known test user)
/// since VisitsController sits behind the same JWT-protected fallback policy.
/// </summary>
public class VisitsEndpointsTests : IDisposable
{
    private readonly AuthWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateVisit_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId = Guid.NewGuid(),
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateVisit_MinimalAllNotRecordedPayload_Returns201()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        var response = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateVisit_FullPayloadWithAppointment_Returns201AndRetrievableBothWays()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);
        var appointmentId = await CreateAppointmentAsync(client, patientId);

        var response = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            appointmentId,
            temperatureValue = 98.6,
            temperatureNotRecorded = false,
            bloodPressureValue = "120/80",
            bloodPressureNotRecorded = false,
            pulseValue = 72,
            pulseNotRecorded = false,
            complaints = "Fever",
            diagnosis = "Viral infection"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var getByIdResponse = await client.GetAsync($"/api/visits/{id}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/visits?patientId={patientId}");
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = listBody.EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("Viral infection", items[0].GetProperty("diagnosis").GetString());
    }

    [Fact]
    public async Task CreateVisit_WithUnknownPatientId_ReturnsBadRequest()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId = Guid.NewGuid(),
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateVisit_WithUnknownAppointmentId_ReturnsBadRequest()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        var response = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            appointmentId = Guid.NewGuid(),
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForUnknownId_ReturnsNotFound()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/visits/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByPatientId_ForPatientWithNoVisits_ReturnsEmptyArray()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        var response = await client.GetAsync($"/api/visits?patientId={patientId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(body.EnumerateArray());
    }

    [Fact]
    public async Task UpdateVisit_PersistsVitalsComplaintsDiagnosisChanges()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);
        var createResponse = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/visits/{id}", new
        {
            temperatureValue = 101.0,
            temperatureNotRecorded = false,
            bloodPressureValue = "140/90",
            bloodPressureNotRecorded = false,
            pulseValue = 88,
            pulseNotRecorded = false,
            complaints = "Cough",
            diagnosis = "Bronchitis"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cough", body.GetProperty("complaints").GetString());
        Assert.Equal("Bronchitis", body.GetProperty("diagnosis").GetString());
        Assert.Equal("140/90", body.GetProperty("bloodPressureValue").GetString());
    }

    [Fact]
    public async Task CreateVisit_WithMedicationList_PersistedAndRetrievableInOrder()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        var response = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true,
            medications = new[]
            {
                new { name = "Amoxicillin", dosage = "250mg", frequency = "Thrice daily", duration = "7 days", instructions = "After food" },
                new { name = "Cetirizine", dosage = "10mg", frequency = "Once daily", duration = "3 days", instructions = "At night" }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var getByIdResponse = await client.GetAsync($"/api/visits/{id}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);
        var body = await getByIdResponse.Content.ReadFromJsonAsync<JsonElement>();
        var meds = body.GetProperty("medications").EnumerateArray().ToList();
        Assert.Equal(2, meds.Count);
        Assert.Equal("Amoxicillin", meds[0].GetProperty("name").GetString());
        Assert.Equal("Cetirizine", meds[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateVisit_WithMalformedMedicationRow_ReturnsBadRequest()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        var response = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true,
            medications = new[]
            {
                new { name = "", dosage = "250mg", frequency = "Thrice daily", duration = "7 days", instructions = "After food" }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateVisit_ReplacesMedicationSet_AddRemoveEdit()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);
        var createResponse = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true,
            medications = new[]
            {
                new { name = "KeepMe", dosage = "1mg", frequency = "Daily", duration = "1 day", instructions = "Now" },
                new { name = "RemoveMe", dosage = "2mg", frequency = "Daily", duration = "1 day", instructions = "Now" }
            }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/visits/{id}", new
        {
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true,
            medications = new[]
            {
                new { name = "KeepMe", dosage = "9mg", frequency = "Daily", duration = "1 day", instructions = "Now" },
                new { name = "NewOne", dosage = "3mg", frequency = "Daily", duration = "1 day", instructions = "Now" }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var getByIdResponse = await client.GetAsync($"/api/visits/{id}");
        var body = await getByIdResponse.Content.ReadFromJsonAsync<JsonElement>();
        var meds = body.GetProperty("medications").EnumerateArray().ToList();
        Assert.Equal(2, meds.Count);
        Assert.Equal("KeepMe", meds[0].GetProperty("name").GetString());
        Assert.Equal("9mg", meds[0].GetProperty("dosage").GetString());
        Assert.Equal("NewOne", meds[1].GetProperty("name").GetString());
        Assert.DoesNotContain(meds, m => m.GetProperty("name").GetString() == "RemoveMe");
    }

    [Fact]
    public async Task GetByPatientId_WithDateRange_ReturnsOnlyVisitsWithinRange()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        // Create three visits; each is timestamped "now" at creation (no way to backdate via the
        // API), so this test filters a range that includes "now" and asserts the created visit is
        // returned, plus a range that excludes it returns empty — this exercises the real filter
        // predicate without needing DB-level seeding of historical dates.
        var createResponse = await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var today = DateTime.UtcNow.Date;
        var inRangeResponse = await client.GetAsync($"/api/visits?patientId={patientId}&fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, inRangeResponse.StatusCode);
        var inRangeBody = await inRangeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(inRangeBody.EnumerateArray());

        var pastDate = today.AddYears(-1);
        var outOfRangeResponse = await client.GetAsync($"/api/visits?patientId={patientId}&fromDate={pastDate:yyyy-MM-dd}&toDate={pastDate:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, outOfRangeResponse.StatusCode);
        var outOfRangeBody = await outOfRangeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(outOfRangeBody.EnumerateArray());
    }

    [Fact]
    public async Task GetByPatientId_NoDateParams_ReturnsIdenticalToPreModule6Behavior()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);
        await client.PostAsJsonAsync("/api/visits", new
        {
            patientId,
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true
        });

        var response = await client.GetAsync($"/api/visits?patientId={patientId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(body.EnumerateArray());
    }

    [Fact]
    public async Task GetByPatientId_FromDateAfterToDate_ReturnsBadRequest()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        var response = await client.GetAsync($"/api/visits?patientId={patientId}&fromDate=2026-08-20&toDate=2026-08-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetByPatientId_MalformedDateQueryString_ReturnsBadRequest()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        var response = await client.GetAsync($"/api/visits?patientId={patientId}&fromDate=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetByPatientId_WithDateRange_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/visits?patientId={Guid.NewGuid()}&fromDate=2026-08-01&toDate=2026-08-31");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateVisit_ForUnknownId_ReturnsNotFound()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync($"/api/visits/{Guid.NewGuid()}", new
        {
            temperatureNotRecorded = true,
            bloodPressureNotRecorded = true,
            pulseNotRecorded = true
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<Guid> CreatePatientAsync(HttpClient client, string fullName = "Jane Doe")
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

    private static async Task<Guid> CreateAppointmentAsync(HttpClient client, Guid patientId)
    {
        var response = await client.PostAsJsonAsync("/api/appointments", new { patientId, appointmentDate = "2026-08-26", appointmentTime = "09:00" });
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
