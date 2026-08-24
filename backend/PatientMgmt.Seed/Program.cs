using Microsoft.EntityFrameworkCore;
using PatientMgmt.BusinessLogic.Auth;
using PatientMgmt.DataAccess;
using PatientMgmt.Domain.Entities;

// Provisioning script (not a UI) that creates the single doctor account, per Module 1
// Business Rule: "Exactly one user account exists in Phase 1; there is no in-app account
// creation UI." Run manually by the dev/deploy team during setup, e.g.:
//   dotnet run --project PatientMgmt.Seed -- "doctor@example.com" "doctor" "TempP@ssw0rd!"
//   (connection string read from PATIENTMGMT_CONNECTION env var, or falls back to LocalDB)

var email = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SEED_USER_EMAIL");
var username = args.Length > 1 ? args[1] : Environment.GetEnvironmentVariable("SEED_USER_USERNAME");
var password = args.Length > 2 ? args[2] : Environment.GetEnvironmentVariable("SEED_USER_PASSWORD");

if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
{
    Console.Error.WriteLine("Usage: PatientMgmt.Seed <email> [username] <password>");
    Console.Error.WriteLine("(or set SEED_USER_EMAIL / SEED_USER_USERNAME / SEED_USER_PASSWORD env vars)");
    return 1;
}

var connectionString = Environment.GetEnvironmentVariable("PATIENTMGMT_CONNECTION")
    ?? "Server=(localdb)\\mssqllocaldb;Database=PatientMgmt;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer(connectionString);

using var db = new AppDbContext(optionsBuilder.Options);

// Ensure schema exists (applies pending migrations); safe to run repeatedly.
db.Database.Migrate();

if (await db.Users.AnyAsync())
{
    Console.WriteLine("A user already exists. Seed is a one-time, single-account operation; no changes made.");
    return 0;
}

var hasher = new PasswordHasher();
var user = new User
{
    Id = Guid.NewGuid(),
    Email = email.Trim(),
    Username = string.IsNullOrWhiteSpace(username) ? null : username.Trim(),
    PasswordHash = hasher.Hash(password),
    CreatedAt = DateTime.UtcNow
};

db.Users.Add(user);
await db.SaveChangesAsync();

Console.WriteLine($"Provisioned doctor account for {user.Email}. Please share the password out-of-band and require a reset on first login if policy dictates.");

// Module 2: seed sample patients so downstream modules (Appointment, Consultation, etc.)
// have data to build/test against (plan §9 task #12). No-op if patients already exist.
await PatientMgmt.Seed.PatientSeedData.SeedAsync(db);

return 0;
