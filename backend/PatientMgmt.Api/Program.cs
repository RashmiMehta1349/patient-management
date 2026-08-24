using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PatientMgmt.Api.Middleware;
using PatientMgmt.BusinessLogic.Auth;
using PatientMgmt.BusinessLogic.Interfaces;
using PatientMgmt.BusinessLogic.Patients;
using PatientMgmt.DataAccess;
using PatientMgmt.DataAccess.Repositories;
using PatientMgmt.Domain.Options;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration binding ----
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
if (string.IsNullOrWhiteSpace(authOptions.JwtSigningKey) && !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException("Auth:JwtSigningKey must be configured (e.g. via environment variable or user-secrets) outside Development.");
}

// ---- Data access tier ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();

// ---- Business-logic tier ----
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IPatientService, PatientService>();

// ---- API tier ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT bearer: validates signature/expiry/issuer/audience. Session (idle-timeout, revocation)
// is enforced separately by JwtSessionMiddleware after this succeeds.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = authOptions.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                string.IsNullOrEmpty(authOptions.JwtSigningKey) ? "DEV_ONLY_INSECURE_KEY_CHANGE_ME_1234567890" : authOptions.JwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// A7: basic rate limiting on public auth endpoints (defensive coding minimum, not a lockout feature).
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    rateLimiterOptions.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authOptions.RateLimitMaxAttempts,
                Window = TimeSpan.FromMinutes(authOptions.RateLimitWindowMinutes),
                QueueLimit = 0
            }));
});

// HSTS + HTTPS enforcement (BRD Security NFR: encryption in transit end-to-end).
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

// CORS: allow the Angular dev/prod origin(s), credentials not needed (bearer token in header, not cookie).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseJwtSessionValidation(); // shared session-revocation/idle-timeout gate, all protected endpoints ride on this
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
