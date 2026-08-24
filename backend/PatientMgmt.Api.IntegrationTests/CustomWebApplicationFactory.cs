using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatientMgmt.BusinessLogic.Auth;
using PatientMgmt.DataAccess;

namespace PatientMgmt.Api.IntegrationTests
{
    /// <summary>
    /// Swaps the real SQL Server DbContext for EF Core InMemory and supplies test-only
    /// config (JWT signing key, short idle timeout where needed) so the full API pipeline
    /// (including JwtSessionMiddleware) can be exercised without a real database.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public readonly string DbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:JwtSigningKey"] = "INTEGRATION_TEST_SIGNING_KEY_1234567890_ABCDEF",
                    ["Auth:IdleTimeoutMinutes"] = "15",
                    ["Auth:SessionHardExpiryMinutes"] = "720",
                    ["Auth:ResetTokenLifetimeMinutes"] = "30",
                    // High limit here: rate limiting itself is covered by RateLimitingTests,
                    // which builds its own factory instance with a low limit; other test
                    // classes share this factory/instance and must not trip the limiter.
                    ["Auth:RateLimitMaxAttempts"] = "1000",
                    ["Auth:RateLimitWindowMinutes"] = "15",
                    ["Cors:AllowedOrigins:0"] = "https://localhost:4200"
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DbName);
                });

                // Never send real email during tests; capture instead.
                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
                if (emailDescriptor is not null)
                {
                    services.Remove(emailDescriptor);
                }
                services.AddSingleton<IEmailSender, CapturingEmailSender>();
            });
        }
    }
}
