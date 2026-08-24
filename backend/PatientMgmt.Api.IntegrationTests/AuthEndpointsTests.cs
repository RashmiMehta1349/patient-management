using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PatientMgmt.BusinessLogic.Auth;
using PatientMgmt.DataAccess;
using PatientMgmt.Domain.Contracts;
using PatientMgmt.Domain.Entities;
using Xunit;

namespace PatientMgmt.Api.IntegrationTests
{
    /// <summary>
    /// Full-pipeline tests per the plan's Test Strategy (API tier + test database):
    /// login happy/unhappy path, protected-call auth gate, forgot/reset with no
    /// enumeration leak, and session invalidation after password reset.
    /// </summary>
    public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string SeedEmail = "doctor@example.com";
        private const string SeedPassword = "CorrectHorseBattery1!";

        public AuthEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            SeedUser();
        }

        private void SeedUser()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (db.Users.Any()) return;

            var hasher = new PasswordHasher();
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = SeedEmail,
                Username = "doctor",
                PasswordHash = hasher.Hash(SeedPassword),
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsTokenAndOk()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest(SeedEmail, SeedPassword));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsGenericUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest(SeedEmail, "WrongPassword!"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<MessageResponse>();
            Assert.Equal("Invalid username or password.", body!.Message);
        }

        [Fact]
        public async Task Session_WithValidToken_ReturnsAuthenticatedTrue()
        {
            var client = _factory.CreateClient();
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(SeedEmail, SeedPassword));
            var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
            var response = await client.GetAsync("/api/v1/auth/session");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<SessionCheckResponse>();
            Assert.True(body!.Authenticated);
        }

        [Fact]
        public async Task Session_WithoutToken_Returns401()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/v1/auth/session");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ForgotPassword_ExistingAndNonExistingEmail_ReturnIdenticalResponseShape()
        {
            var client = _factory.CreateClient();

            var existing = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(SeedEmail));
            var nonExisting = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest("nobody@example.com"));

            Assert.Equal(existing.StatusCode, nonExisting.StatusCode);
            var existingBody = await existing.Content.ReadFromJsonAsync<MessageResponse>();
            var nonExistingBody = await nonExisting.Content.ReadFromJsonAsync<MessageResponse>();
            Assert.Equal(existingBody!.Message, nonExistingBody!.Message);
        }

        [Fact]
        public async Task ResetPassword_ValidToken_InvalidatesPriorSession()
        {
            var client = _factory.CreateClient();

            // 1. Log in, obtain a token bound to session A.
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(SeedEmail, SeedPassword));
            var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
            var preResetToken = loginBody!.AccessToken;

            // 2. Request reset; capture the emailed link via the test double.
            await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(SeedEmail));

            using var scope = _factory.Services.CreateScope();
            var capturingSender = (CapturingEmailSender)scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var captured = capturingSender.SentEmails.Last();
            var rawToken = new Uri(captured.ResetLink).Query.Split("token=")[1];
            rawToken = Uri.UnescapeDataString(rawToken);

            // 3. Complete reset with a new password.
            var resetResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
                new ResetPasswordRequest(rawToken, "BrandNewPassword2!"));
            Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

            // 4. The pre-reset JWT must now be rejected (session invalidated).
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", preResetToken);
            var sessionCheck = await client.GetAsync("/api/v1/auth/session");
            Assert.Equal(HttpStatusCode.Unauthorized, sessionCheck.StatusCode);

            // 5. Old password no longer works; new password does.
            client.DefaultRequestHeaders.Authorization = null;
            var oldLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(SeedEmail, SeedPassword));
            Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

            var newLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(SeedEmail, "BrandNewPassword2!"));
            Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        }

        [Fact]
        public async Task Logout_InvalidatesSession()
        {
            var client = _factory.CreateClient();
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(SeedEmail, SeedPassword));
            var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
            var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
            Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

            var sessionCheck = await client.GetAsync("/api/v1/auth/session");
            Assert.Equal(HttpStatusCode.Unauthorized, sessionCheck.StatusCode);
        }
    }
}
