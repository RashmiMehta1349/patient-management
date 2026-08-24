using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Commands;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Commands;
using PatientManagement.Application.Patients.Queries;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Infrastructure.Persistence;
using PatientManagement.Infrastructure.Repositories;
using PatientManagement.Infrastructure.Services;

namespace PatientManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Server=(localdb)\\mssqllocaldb;Database=PatientManagement;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<PatientManagementDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();

        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IEmailSender, DevConsoleEmailSender>();
        services.AddSingleton<IResetTokenGenerator, ResetTokenGenerator>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<ForgotPasswordCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();
        services.AddScoped<CreatePatientCommandHandler>();
        services.AddScoped<GetPatientByIdQueryHandler>();
        services.AddScoped<UpdatePatientCommandHandler>();

        return services;
    }
}
