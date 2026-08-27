using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace PatientManagement.Tests.Integration.Auth;

/// <summary>
/// Each test method gets its own <see cref="AuthWebApplicationFactory"/> (and therefore its own
/// isolated in-memory SQLite database) rather than sharing one via IClassFixture — several tests
/// mutate the seeded user's password, which would otherwise leak across tests and cause
/// order-dependent flakiness.
/// </summary>
public class AuthEndpointsTests : IDisposable
{
    private readonly AuthWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Login_WithSeededCredentials_ReturnsToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = AuthWebApplicationFactory.TestUserEmail,
            password = AuthWebApplicationFactory.TestUserPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = AuthWebApplicationFactory.TestUserEmail,
            password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserInfo()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(AuthWebApplicationFactory.TestUserEmail, body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTamperedToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token + "tampered");
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ReturnsOkWithValidTokenAnd401Without()
    {
        var client = _factory.CreateClient();

        var withoutToken = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, withoutToken.StatusCode);

        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var withToken = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, withToken.StatusCode);
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
