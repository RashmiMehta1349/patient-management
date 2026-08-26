using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PatientManagement.Infrastructure.Persistence;
using PatientManagement.Tests.Integration.Auth;
using Xunit;

namespace PatientManagement.Tests.Integration.Patients;

/// <summary>
/// Reuses <see cref="AuthWebApplicationFactory"/> (isolated in-memory SQLite DB seeded with a
/// known test user) since PatientsController sits behind the same JWT-protected fallback policy.
/// </summary>
public class PatientsEndpointsTests : IDisposable
{
    private readonly AuthWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetPatientById_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/patients/{Random.Shared.NextInt64(1, long.MaxValue)}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPatientById_ForCreatedPatient_ReturnsMatchingFields()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/patients", ValidPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/patients/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Jane Doe", body.GetProperty("fullName").GetString());
        Assert.Equal("555-123-4567", body.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task GetPatientById_ForUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/patients/{Random.Shared.NextInt64(1, long.MaxValue)}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePatient_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/patients/{Random.Shared.NextInt64(1, long.MaxValue)}", ValidPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePatient_WithValidPayload_PersistsChangeVisibleOnSubsequentGet()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/patients", ValidPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var updateResponse = await client.PutAsJsonAsync($"/api/patients/{id}", new
        {
            fullName = "Jane Doe",
            dateOfBirth = "1990-05-15",
            gender = "Female",
            phoneNumber = "555-999-0000"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("555-999-0000", updatedBody.GetProperty("phoneNumber").GetString());

        var getResponse = await client.GetAsync($"/api/patients/{id}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("555-999-0000", getBody.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task UpdatePatient_WithInvalidPayload_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/patients", ValidPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/patients/{id}", new
        {
            fullName = "",
            dateOfBirth = "1990-05-15",
            gender = "Female",
            phoneNumber = "555-999-0000"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePatient_ForUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync($"/api/patients/{Random.Shared.NextInt64(1, long.MaxValue)}", ValidPayload());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePatient_LocationHeaderResolvesViaGetById()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/patients", ValidPayload());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        var followResponse = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, followResponse.StatusCode);
    }

    [Fact]
    public async Task CreatePatient_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/patients", ValidPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePatient_WithValidTokenAndPayload_Returns201AndPatientIsImmediatelyRetrievable()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/patients", ValidPayload());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = long.Parse(body.GetProperty("id").GetString()!);
        Assert.Equal("Jane Doe", body.GetProperty("fullName").GetString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PatientManagementDbContext>();
        var savedPatient = await dbContext.Patients.FirstOrDefaultAsync(p => p.Id == id);

        Assert.NotNull(savedPatient);
        Assert.Equal("Jane Doe", savedPatient!.FullName);
        Assert.Equal("Female", savedPatient.Gender);
        Assert.Equal("555-123-4567", savedPatient.PhoneNumber);
    }

    [Fact]
    public async Task CreatePatient_WithMissingRequiredField_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            fullName = "",
            dateOfBirth = "1990-05-15",
            gender = "Female",
            phoneNumber = "555-123-4567"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/patients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_BrowseAll_ReturnsPagedEnvelopeOrderedByName()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/api/patients", NamedPayload("Zack Adams"));
        await client.PostAsJsonAsync("/api/patients", NamedPayload("Amy Baker"));

        var response = await client.GetAsync("/api/patients?page=1&pageSize=25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(25, body.GetProperty("pageSize").GetInt32());
        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.True(items.Count >= 2);
        var names = items.Select(i => i.GetProperty("fullName").GetString()).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task GetAll_Page2With15Seeded_ReturnsRemaining5()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < 15; i++)
        {
            await client.PostAsJsonAsync("/api/patients", NamedPayload($"Patient {i:D2}"));
        }

        var response = await client.GetAsync("/api/patients?page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(15, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(5, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetAll_PageFarBeyondRange_ReturnsEmptyItemsNot404()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/api/patients", NamedPayload("Only Patient"));

        var response = await client.GetAsync("/api/patients?page=99&pageSize=25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(body.GetProperty("items").EnumerateArray());
        Assert.True(body.GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task GetAll_EmptyQueryString_BehavesIdenticallyToNoQuery()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/api/patients", NamedPayload("Query Regression"));

        var response = await client.GetAsync("/api/patients?query=&page=1&pageSize=25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("fullName").GetString());
        Assert.Contains("Query Regression", names);
    }

    [Fact]
    public async Task GetAll_WithQueryMatchingMoreThanPageSize_ReturnsPagedMatches()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < 7; i++)
        {
            await client.PostAsJsonAsync("/api/patients", NamedPayload($"Searchable Patient {i:D2}"));
        }

        var response = await client.GetAsync("/api/patients?query=Searchable&page=2&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetAll_PageSizeAboveMax_EchoesClampedPageSize()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/patients?pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, body.GetProperty("pageSize").GetInt32());
    }

    private static object NamedPayload(string fullName) => new
    {
        fullName,
        dateOfBirth = "1990-05-15",
        gender = "Female",
        phoneNumber = "555-123-4567"
    };

    private static object ValidPayload() => new
    {
        fullName = "Jane Doe",
        dateOfBirth = "1990-05-15",
        gender = "Female",
        phoneNumber = "555-123-4567"
    };

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
