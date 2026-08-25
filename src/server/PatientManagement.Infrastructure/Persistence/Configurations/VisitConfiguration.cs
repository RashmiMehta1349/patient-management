using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Infrastructure.Persistence.Configurations;

public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("Visits");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.PatientId).IsRequired();
        builder.HasIndex(v => v.PatientId);
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(v => v.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.AppointmentId).IsRequired(false);
        builder.HasIndex(v => v.AppointmentId);
        builder.HasOne<Appointment>()
            .WithMany()
            .HasForeignKey(v => v.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.VisitDate).IsRequired();

        builder.Property(v => v.TemperatureValue).HasColumnType("decimal(4,1)");
        builder.Property(v => v.TemperatureNotRecorded).IsRequired();

        builder.Property(v => v.BloodPressureValue).HasMaxLength(20);
        builder.Property(v => v.BloodPressureNotRecorded).IsRequired();

        builder.Property(v => v.PulseValue);
        builder.Property(v => v.PulseNotRecorded).IsRequired();

        builder.Property(v => v.Complaints).HasMaxLength(2000);
        builder.Property(v => v.Diagnosis).HasMaxLength(2000);

        builder.Property(v => v.CreatedAt).IsRequired();
        builder.Property(v => v.UpdatedAt).IsRequired();
    }
}
