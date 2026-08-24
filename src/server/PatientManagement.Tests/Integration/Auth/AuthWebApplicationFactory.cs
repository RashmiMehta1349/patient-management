using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Infrastructure.Persistence;
using PatientManagement.Infrastructure.Seed;

namespace PatientManagement.Tests.Integration.Auth;

/// <summary>
/// Spins up the full API pipeline against an isolated SQLite in-memory database per factory
/// instance, seeded with a known test user, for end-to-end auth endpoint testing.
/// </summary>
public class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestUserEmail = "test-doctor@example.com";
    public const string TestUserPassword = "TestPassword123!";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
            {
                ["Auth:JwtSigningKey"] = "integration-test-signing-key-please-32chars-min",
                ["Auth:AccessTokenLifetimeMinutes"] = "20",
                ["Auth:ResetTokenLifetimeMinutes"] = "30",
                ["Seed:UserEmail"] = TestUserEmail,
                ["Seed:UserPassword"] = TestUserPassword
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PatientManagementDbContext>));
            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<PatientManagementDbContext>(options =>
                options.UseSqlite(_connection));

            var emailSenderDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
            if (emailSenderDescriptor is not null)
            {
                services.Remove(emailSenderDescriptor);
            }
            services.AddSingleton<IEmailSender>(EmailSender);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
