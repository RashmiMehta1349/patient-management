using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
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
    /// Full-pipeline tests per the Module 2 plan's Test Strategy (§12): create → immediate
    /// retrieve, missing-field validation, edit persists, search by partial name/phone,
    /// duplicate-check both cases, unauthenticated calls return 401.
    /// </summary>
    public class PatientsControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private const string SeedEmail = "doctor2@example.com";
        private const string SeedPassword = "CorrectHorseBattery1!";

        public PatientsControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            SeedUser();
        }

        private void SeedUser()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (db.Users.Any(u => u.Email == SeedEmail)) return;

            var hasher = new PasswordHasher();
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = SeedEmail,
                Username = "doctor2",
                PasswordHash = hasher.Hash(SeedPassword),
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        private async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            var client = _factory.CreateClient();
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(SeedEmail, SeedPassword));
            var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
            return client;
        }

        private static CreatePatientRequest MakeValidRequest(string name, string phone) =>
            new(name, new DateTime(1990, 5, 1), null, "Female", phone, null, null);

        [Fact]
        public async Task Create_ValidPayload_ReturnsCreatedAndImmediatelyRetrievable()
        {
            var client = await CreateAuthenticatedClientAsync();

            var response = await client.PostAsJsonAsync("/api/v1/patients", MakeValidRequest("Alice Wonderland", "5551110001"));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<PatientResponse>();
            Assert.NotNull(created);
            Assert.False(string.IsNullOrWhiteSpace(created!.PatientCode));

            var getResponse = await client.GetAsync($"/api/v1/patients/{created.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var fetched = await getResponse.Content.ReadFromJsonAsync<PatientResponse>();
            Assert.Equal("Alice Wonderland", fetched!.FullName);
        }

        [Fact]
        public async Task Create_MissingRequiredField_Returns400()
        {
            var client = await CreateAuthenticatedClientAsync();

            var response = await client.PostAsJsonAsync("/api/v1/patients",
                new CreatePatientRequest("", new DateTime(1990, 5, 1), null, "Female", "5551110002", null, null));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Update_PersistsChanges()
        {
            var client = await CreateAuthenticatedClientAsync();
            var create = await client.PostAsJsonAsync("/api/v1/patients", MakeValidRequest("Bob Builder", "5551110003"));
            var created = await create.Content.ReadFromJsonAsync<PatientResponse>();

            var updateRequest = new UpdatePatientRequest("Bob Builder", new DateTime(1990, 5, 1), null, "Male", "5559998888", null, null);
            var updateResponse = await client.PutAsJsonAsync($"/api/v1/patients/{created!.Id}", updateRequest);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var getResponse = await client.GetAsync($"/api/v1/patients/{created.Id}");
            var fetched = await getResponse.Content.ReadFromJsonAsync<PatientResponse>();
            Assert.Equal("5559998888", fetched!.PhoneNumber);
        }

        [Fact]
        public async Task Search_ByPartialName_ReturnsMatchingPatientsOnly()
        {
            var client = await CreateAuthenticatedClientAsync();
            await client.PostAsJsonAsync("/api/v1/patients", MakeValidRequest("Zelda Fitzgerald", "5552220001"));
            await client.PostAsJsonAsync("/api/v1/patients", MakeValidRequest("Unrelated Person", "5552220002"));

            var response = await client.GetAsync("/api/v1/patients?search=Zelda");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();

            Assert.Contains(results!, p => p.FullName == "Zelda Fitzgerald");
            Assert.DoesNotContain(results!, p => p.FullName == "Unrelated Person");
        }

        [Fact]
        public async Task Search_ByPartialPhone_ReturnsMatchingPatientsOnly()
        {
            var client = await CreateAuthenticatedClientAsync();
            await client.PostAsJsonAsync("/api/v1/patients", MakeValidRequest("Carl Sagan", "5553330099"));
            await client.PostAsJsonAsync("/api/v1/patients", MakeValidRequest("Other Person", "5559990000"));

            var response = await client.GetAsync("/api/v1/patients?search=333009");
            var results = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();

            Assert.Contains(results!, p => p.FullName == "Carl Sagan");
            Assert.DoesNotContain(results!, p => p.FullName == "Other Person");
        }

        [Fact]
        public async Task CheckDuplicate_ExistingNameAndPhone_ReturnsWarningTrue()
        {
            var client = await CreateAuthenticatedClientAsync();
            await client.PostAsJsonAsync("/api/v1/patients", MakeValidRequest("Diana Prince", "5554440001"));

            var response = await client.GetAsync("/api/v1/patients/check-duplicate?name=Diana%20Prince&phone=5554440001");
            var body = await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>();

            Assert.True(body!.PossibleDuplicate);
        }

        [Fact]
        public async Task CheckDuplicate_UniqueNameAndPhone_ReturnsWarningFalse()
        {
            var client = await CreateAuthenticatedClientAsync();

            var response = await client.GetAsync("/api/v1/patients/check-duplicate?name=Nobody%20Special&phone=5550000999");
            var body = await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>();

            Assert.False(body!.PossibleDuplicate);
        }

        [Fact]
        public async Task AllEndpoints_WithoutBearerToken_Return401()
        {
            var client = _factory.CreateClient();
            var id = Guid.NewGuid();

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/patients", MakeValidRequest("X", "5550001111"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/patients/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync($"/api/v1/patients/{id}", MakeValidRequest("X", "5550001111"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/patients?search=x")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/patients/check-duplicate?name=x&phone=1234567")).StatusCode);
        }

        [Fact]
        public async Task Search_And_GetById_CompleteWithinSuccessCriteriaLatency()
        {
            // Basic timing check per plan §12 Performance (not full load testing).
            var client = await CreateAuthenticatedClientAsync();
            var create = await client.PostAsJsonAsync("/api/v1/patients", MakeValidRequest("Perf Test Patient", "5556660000"));
            var created = await create.Content.ReadFromJsonAsync<PatientResponse>();

            var sw = Stopwatch.StartNew();
            var getResponse = await client.GetAsync($"/api/v1/patients/{created!.Id}");
            sw.Stop();
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(sw.Elapsed.TotalSeconds < 5, $"GetById took {sw.Elapsed.TotalSeconds}s, exceeding the 2-5s Success Criteria.");

            sw.Restart();
            var searchResponse = await client.GetAsync("/api/v1/patients?search=Perf");
            sw.Stop();
            Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
            Assert.True(sw.Elapsed.TotalSeconds < 5, $"Search took {sw.Elapsed.TotalSeconds}s, exceeding the 2-5s Success Criteria.");
        }
    }
}
