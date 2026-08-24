using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Domain.Entities;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.Seed;

/// <summary>
/// Creates the single pre-provisioned physician account from configuration/environment
/// variables at startup, if it does not already exist. This is the only way an account is
/// created in Phase 1 — there is intentionally no in-app account-creation API.
///
/// Reads:
///   SEED_USER_EMAIL     (required to seed)
///   SEED_USER_PASSWORD  (required to seed)
/// via configuration (environment variables or appsettings), never hardcoded.
/// </summary>
public static class UserSeeder
{
    public static async Task SeedAsync(
        PatientManagementDbContext dbContext,
        IPasswordHasherService passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        if (dbContext.Users.Any())
        {
            return;
        }

        var email = configuration["SEED_USER_EMAIL"] ?? configuration["Seed:UserEmail"];
        var password = configuration["SEED_USER_PASSWORD"] ?? configuration["Seed:UserPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No provisioned user exists and SEED_USER_EMAIL/SEED_USER_PASSWORD are not configured. " +
                "Skipping seed — login will fail until an account is seeded.");
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            PasswordHash = passwordHasher.HashPassword(password),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded pre-provisioned user account for {Email}.", user.Email);
    }
}
