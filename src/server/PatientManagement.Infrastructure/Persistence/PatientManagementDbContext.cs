using Microsoft.EntityFrameworkCore;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Infrastructure.Persistence;

public class PatientManagementDbContext : DbContext
{
    public PatientManagementDbContext(DbContextOptions<PatientManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<Visit> Visits => Set<Visit>();

    public DbSet<Medication> Medications => Set<Medication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientManagementDbContext).Assembly);
    }
}
