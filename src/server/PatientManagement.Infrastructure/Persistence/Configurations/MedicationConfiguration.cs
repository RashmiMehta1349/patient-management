using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientManagement.Domain.Entities;

namespace PatientManagement.Infrastructure.Persistence.Configurations;

public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("Medications");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.VisitId).IsRequired();
        builder.HasIndex(m => m.VisitId);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Dosage).HasMaxLength(100);
        builder.Property(m => m.Frequency).HasMaxLength(100);
        builder.Property(m => m.Duration).HasMaxLength(100);
        builder.Property(m => m.Instructions).HasMaxLength(500);

        builder.Property(m => m.SortOrder).IsRequired();

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        // Medications have no meaning independent of their parent Visit — unlike Visit's own FKs
        // to Patient/Appointment (Restrict), a medication is deleted along with its visit
        // (approved plan §5).
        builder.HasOne<Visit>()
            .WithMany(v => v.Medications)
            .HasForeignKey(m => m.VisitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
