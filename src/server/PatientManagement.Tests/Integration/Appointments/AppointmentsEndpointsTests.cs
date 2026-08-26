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

namespace PatientManagement.Tests.Integration.Appointments;

/// <summary>
/// Reuses AuthWebApplicationFactory (isolated in-memory SQLite DB seeded with a known test user)
/// since AppointmentsController sits behind the same JWT-protected fallback policy.
/// </summary>
public class AppointmentsEndpointsTests : IDisposable
{
    private readonly AuthWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateAppointment_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/appointments", new { patientId = Random.Shared.NextInt64(1, long.MaxValue), appointmentDate = "2026-08-26", appointmentTime = "09:00" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByDate_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/appointments?date=2026-08-26");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_WithValidPayload_Returns201AndAppearsInSameDaysList()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        var response = await client.PostAsJsonAsync("/api/appointments", new { patientId, appointmentDate = "2026-08-26", appointmentTime = "09:00" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var listResponse = await client.GetAsync("/api/appointments?date=2026-08-26");
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = listBody.EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("09:00", items[0].GetProperty("appointmentTime").GetString());
    }

    [Fact]
    public async Task CreateAppointment_WithUnknownPatientId_ReturnsBadRequest()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/appointments", new { patientId = Random.Shared.NextInt64(1, long.MaxValue), appointmentDate = "2026-08-26", appointmentTime = "09:00" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_OverlappingExisting_Returns201WithOverlapWarning()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        await client.PostAsJsonAsync("/api/appointments", new { patientId, appointmentDate = "2026-08-26", appointmentTime = "09:00" });
        var response = await client.PostAsJsonAsync("/api/appointments", new { patientId, appointmentDate = "2026-08-26", appointmentTime = "09:15" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("hasOverlapWarning").GetBoolean());
    }

    [Fact]
    public async Task GetByDate_OnlyReturnsThatDaysAppointmentsInTimeOrder()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);

        await client.PostAsJsonAsync("/api/appointments", new { patientId, appointmentDate = "2026-08-26", appointmentTime = "10:00" });
        await client.PostAsJsonAsync("/api/appointments", new { patientId, appointmentDate = "2026-08-26", appointmentTime = "08:00" });
        await client.PostAsJsonAsync("/api/appointments", new { patientId, appointmentDate = "2026-08-27", appointmentTime = "09:00" });

        var response = await client.GetAsync("/api/appointments?date=2026-08-26");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.EnumerateArray().ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("08:00", items[0].GetProperty("appointmentTime").GetString());
        Assert.Equal("10:00", items[1].GetProperty("appointmentTime").GetString());
    }

    [Fact]
    public async Task UpdateStatus_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsync($"/api/appointments/{Random.Shared.NextInt64(1, long.MaxValue)}/status",
            JsonContent.Create(new { status = "Completed" }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WithValidStatus_ReturnsOkAndSubsequentGetReflectsIt()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);
        var createResponse = await client.PostAsJsonAsync("/api/appointments", new { patientId, appointmentDate = "2026-08-26", appointmentTime = "09:00" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.PatchAsync($"/api/appointments/{id}/status", JsonContent.Create(new { status = "Completed" }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/appointments/{id}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Completed", getBody.GetProperty("status").GetString());
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);
        var createResponse = await client.PostAsJsonAsync("/api/appointments", new { patientId, appointmentDate = "2026-08-26", appointmentTime = "09:00" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.PatchAsync($"/api/appointments/{id}/status", JsonContent.Create(new { status = "Bogus" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ForUnknownId_ReturnsNotFound()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PatchAsync($"/api/appointments/{Random.Shared.NextInt64(1, long.MaxValue)}/status", JsonContent.Create(new { status = "Completed" }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAppointment_PersistsDateTimeNotesChanges()
    {
        var client = await AuthenticatedClientAsync();
        var patientId = await CreatePatientAsync(client);
        var createResponse = await client.PostAsJsonAsync("/api/appointments", new { patientId, appointmentDate = "2026-08-26", appointmentTime = "09:00" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/appointments/{id}", new { appointmentDate = "2026-08-28", appointmentTime = "14:30", notes = "Follow-up" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-08-28", body.GetProperty("appointmentDate").GetString());
        Assert.Equal("14:30", body.GetProperty("appointmentTime").GetString());
        Assert.Equal("Follow-up", body.GetProperty("notes").GetString());
    }

    [Fact]
    public async Task GetByPatientId_ReturnsOnlyThatPatientsAppointments()
    {
        var client = await AuthenticatedClientAsync();
        var patientAId = await CreatePatientAsync(client, "Patient A");
        var patientBId = await CreatePatientAsync(client, "Patient B");

        await client.PostAsJsonAsync("/api/appointments", new { patientId = patientAId, appointmentDate = "2026-08-26", appointmentTime = "09:00" });
        await client.PostAsJsonAsync("/api/appointments", new { patientId = patientBId, appointmentDate = "2026-08-26", appointmentTime = "10:00" });

        var response = await client.GetAsync($"/api/appointments?patientId={patientAId}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.EnumerateArray().ToList();

        Assert.Single(items);
        Assert.Equal(patientAId.ToString(), items[0].GetProperty("patientId").GetString());
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<long> CreatePatientAsync(HttpClient client, string fullName = "Jane Doe")
    {
        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            fullName,
            dateOfBirth = "1990-05-15",
            gender = "Female",
            phoneNumber = "555-123-4567"
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
