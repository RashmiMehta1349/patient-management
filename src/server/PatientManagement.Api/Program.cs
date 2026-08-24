using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Infrastructure;
using PatientManagement.Infrastructure.Persistence;
using PatientManagement.Infrastructure.Seed;
using PatientManagement.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Resolved lazily from DI (via IOptions<AuthOptions>) rather than read directly from
// builder.Configuration at startup, so test hosts (e.g., WebApplicationFactory) that override
// configuration after this point still take effect.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<AuthOptions>>((jwtOptions, authOptionsWrapper) =>
    {
        var authOptions = authOptionsWrapper.Value;
        if (string.IsNullOrWhiteSpace(authOptions.JwtSigningKey))
        {
            // Fail fast rather than silently issuing/validating tokens with an empty/insecure key.
            throw new InvalidOperationException(
                "Auth:JwtSigningKey (or JWT_SIGNING_KEY env var) must be configured.");
        }

        jwtOptions.MapInboundClaims = false;
        jwtOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = authOptions.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.JwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        jwtOptions.Events = new JwtBearerEvents
        {
            // Re-check the SecurityStamp claim against the DB on every request so a password
            // reset immediately invalidates all previously issued tokens.
            OnTokenValidated = async context =>
            {
                var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();

                var subClaim = context.Principal?.FindFirst("sub")?.Value;
                var stampClaim = context.Principal?.FindFirst(AuthClaimTypes.SecurityStamp)?.Value;

                if (subClaim is null || !Guid.TryParse(subClaim, out var userId) || stampClaim is null)
                {
                    context.Fail("Invalid token claims.");
                    return;
                }

                var user = await userRepository.GetByIdAsync(userId, context.HttpContext.RequestAborted);
                if (user is null || user.SecurityStamp != stampClaim)
                {
                    context.Fail("Token has been invalidated.");
                    return;
                }

                user.LastActivityAt = DateTime.UtcNow;
                await userRepository.UpdateAsync(user, context.HttpContext.RequestAborted);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Every application endpoint requires a valid, unexpired, SecurityStamp-matching JWT by
    // default; endpoints opt out explicitly via [AllowAnonymous] (login/forgot/reset only).
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7299;
});

const string DevCorsPolicy = "DevClient";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        // Allows the local Angular dev server (ng serve, default port 4200) to call this API
        // cross-origin; browsers block XHR/fetch across origins without this even when the
        // request itself would otherwise succeed.
        options.AddPolicy(DevCorsPolicy, policy =>
            policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

if (builder.Environment.IsProduction())
{
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
    });
}

var app = builder.Build();

// Apply migrations and seed the pre-provisioned account at startup (dev/demo convenience).
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PatientManagementDbContext>();

    // Integration tests (WebApplicationFactory) swap in an isolated SQLite in-memory DB. Real
    // SQL Server migrations carry provider-specific annotations (e.g. `uniqueidentifier`) that
    // trip EF Core's PendingModelChangesWarning when replayed against a different provider, so
    // the test DB is built straight from the current model instead of the migration history.
    if (app.Environment.IsEnvironment("Testing"))
    {
        dbContext.Database.EnsureCreated();
    }
    else
    {
        dbContext.Database.Migrate();
    }

    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UserSeeder");
    await UserSeeder.SeedAsync(dbContext, passwordHasher, app.Configuration, logger);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

// TestServer (used by WebApplicationFactory-based integration tests) is in-memory and never
// actually serves TLS, so HTTPS redirection is skipped in "Testing". It's also skipped in
// Development: the Angular dev server (environment.development.ts) talks to the API over plain
// HTTP on a fixed local port, and forcing a redirect here would send it to a port Kestrel isn't
// bound on unless the "https" launch profile is used explicitly.
if (!app.Environment.IsEnvironment("Testing") && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
