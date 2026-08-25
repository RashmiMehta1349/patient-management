using System;
using System.Linq;
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
    public async Task ForgotPassword_ThenResetPassword_ThenLoginWithNewPassword_RoundTripSucceeds()
    {
        var client = _factory.CreateClient();

        var forgotResponse = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = AuthWebApplicationFactory.TestUserEmail
        });
        Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);

        var rawToken = ExtractTokenFromLink(_factory.EmailSender.LastResetLink!);
        const string newPassword = "BrandNewPassword456!";

        var resetResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = rawToken,
            newPassword
        });
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        // Old password no longer works.
        var oldLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = AuthWebApplicationFactory.TestUserEmail,
            password = AuthWebApplicationFactory.TestUserPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        // New password works.
        var newLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = AuthWebApplicationFactory.TestUserEmail,
            password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task PasswordReset_InvalidatesTokensIssuedBeforeReset()
    {
        var client = _factory.CreateClient();

        var preResetToken = await LoginAndGetTokenAsync(client);

        var forgotResponse = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = AuthWebApplicationFactory.TestUserEmail
        });
        Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);
        var rawToken = ExtractTokenFromLink(_factory.EmailSender.LastResetLink!);

        var resetResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = rawToken,
            newPassword = "AnotherNewPassword789!"
        });
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", preResetToken);
        var meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    private static string ExtractTokenFromLink(string resetLink)
    {
        var uri = new Uri(resetLink);
        var tokenParam = uri.Query
            .TrimStart('?')
            .Split('&')
            .Select(p => p.Split('=', 2))
            .First(p => p[0] == "token");
        return Uri.UnescapeDataString(tokenParam[1]);
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
