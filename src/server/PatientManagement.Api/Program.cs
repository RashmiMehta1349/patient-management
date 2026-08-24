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
    options.HttpsPort = 5443;
});

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
    dbContext.Database.Migrate();

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
// actually serves TLS, so HTTPS redirection is skipped only in the "Testing" environment —
// every real deployment target (Development/Production) still enforces it.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
