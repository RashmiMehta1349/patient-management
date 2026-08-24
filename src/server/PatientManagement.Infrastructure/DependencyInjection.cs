using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Commands;
using PatientManagement.Application.Auth.Services;
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
            ?? "Data Source=patientmanagement.db";

        services.AddDbContext<PatientManagementDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IEmailSender, DevConsoleEmailSender>();
        services.AddSingleton<IResetTokenGenerator, ResetTokenGenerator>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<ForgotPasswordCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();

        return services;
    }
}
