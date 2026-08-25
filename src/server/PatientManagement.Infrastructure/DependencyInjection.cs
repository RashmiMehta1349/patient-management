using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Appointments.Commands;
using PatientManagement.Application.Appointments.Queries;
using PatientManagement.Application.Appointments.Services;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Commands;
using PatientManagement.Application.Auth.Services;
using PatientManagement.Application.Patients.Commands;
using PatientManagement.Application.Patients.Queries;
using PatientManagement.Application.Patients.Services;
using PatientManagement.Application.Prescriptions.Queries;
using PatientManagement.Application.Prescriptions.Services;
using PatientManagement.Application.Visits.Commands;
using PatientManagement.Application.Visits.Queries;
using PatientManagement.Application.Visits.Services;
using PatientManagement.Infrastructure.Persistence;
using PatientManagement.Infrastructure.Repositories;
using PatientManagement.Infrastructure.Services;
using QuestPDF.Infrastructure;

namespace PatientManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // QuestPDF Community license — free for organizations under the revenue/team-size
        // thresholds in QuestPDF's license terms (https://www.questpdf.com/license/). Flagged for
        // confirmation this project qualifies before any commercial go-live (product decision:
        // server-generated PDF, overriding the plan's original browser-print recommendation).
        QuestPDF.Settings.License = LicenseType.Community;

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<AppointmentOptions>(configuration.GetSection(AppointmentOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Server=(localdb)\\mssqllocaldb;Database=PatientManagement;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<PatientManagementDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();

        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IEmailSender, DevConsoleEmailSender>();
        services.AddSingleton<IResetTokenGenerator, ResetTokenGenerator>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPrescriptionPdfGenerator, QuestPdfPrescriptionGenerator>();

        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<ForgotPasswordCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();
        services.AddScoped<CreatePatientCommandHandler>();
        services.AddScoped<GetPatientByIdQueryHandler>();
        services.AddScoped<UpdatePatientCommandHandler>();
        services.AddScoped<GetAllPatientsQueryHandler>();
        services.AddScoped<SearchPatientsQueryHandler>();
        services.AddScoped<CreateAppointmentCommandHandler>();
        services.AddScoped<GetAppointmentsByDateQueryHandler>();
        services.AddScoped<GetAppointmentByIdQueryHandler>();
        services.AddScoped<UpdateAppointmentStatusCommandHandler>();
        services.AddScoped<UpdateAppointmentCommandHandler>();
        services.AddScoped<GetAppointmentsByPatientIdQueryHandler>();
        services.AddScoped<CreateVisitCommandHandler>();
        services.AddScoped<UpdateVisitCommandHandler>();
        services.AddScoped<GetVisitByIdQueryHandler>();
        services.AddScoped<GetVisitsByPatientIdQueryHandler>();
        services.AddScoped<GetPrescriptionPdfQueryHandler>();

        return services;
    }
}
