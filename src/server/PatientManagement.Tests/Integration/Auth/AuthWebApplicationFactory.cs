using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                ["Seed:UserEmail"] = TestUserEmail,
                ["Seed:UserPassword"] = TestUserPassword
            });
        });

        builder.ConfigureServices(services =>
        {
            // Removing only the DbContextOptions<T> descriptor isn't enough: AddDbContext also
            // registers an internal IDbContextOptionsConfiguration<T> marker recording the
            // SqlServer configuration delegate, and that's additive (TryAddEnumerable) — so a
            // second AddDbContext call for the same context leaves both the SqlServer and
            // SQLite configurations applied together, which EF Core rejects at runtime with
            // "Services for database providers ... have been registered". Strip every
            // descriptor touching PatientManagementDbContext before re-registering it here.
            var dbContextDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<PatientManagementDbContext>) ||
                    d.ServiceType == typeof(PatientManagementDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GenericTypeArguments.Contains(typeof(PatientManagementDbContext))))
                .ToList();
            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<PatientManagementDbContext>(options =>
                options.UseSqlite(_connection));
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
