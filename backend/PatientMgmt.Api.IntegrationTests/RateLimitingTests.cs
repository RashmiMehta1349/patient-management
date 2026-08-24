using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using PatientMgmt.Domain.Contracts;
using Xunit;

namespace PatientMgmt.Api.IntegrationTests
{
    /// <summary>
    /// Own factory instance with a deliberately low rate limit so it doesn't interfere with
    /// (or get interfered with by) the shared-factory tests in AuthEndpointsTests (A7).
    /// </summary>
    public class LowRateLimitWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:RateLimitMaxAttempts"] = "3",
                    ["Auth:RateLimitWindowMinutes"] = "15"
                });
            });
        }
    }

    public class RateLimitingTests : IClassFixture<LowRateLimitWebApplicationFactory>
    {
        private readonly LowRateLimitWebApplicationFactory _factory;

        public RateLimitingTests(LowRateLimitWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Login_ExceedsRateLimitThreshold_Returns429()
        {
            var client = _factory.CreateClient();
            HttpResponseMessage? last = null;

            for (var i = 0; i < 4; i++)
            {
                last = await client.PostAsJsonAsync("/api/v1/auth/login",
                    new LoginRequest("nobody@example.com", "whatever"));
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        }
    }
}
