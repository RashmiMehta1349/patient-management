using Microsoft.EntityFrameworkCore;
using PatientMgmt.Domain.Entities;

namespace PatientMgmt.DataAccess
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(b =>
            {
                b.ToTable("Users");
                b.HasKey(u => u.Id);
                b.Property(u => u.Email).HasMaxLength(256).IsRequired();
                b.HasIndex(u => u.Email).IsUnique();
                b.Property(u => u.Username).HasMaxLength(100);
                b.HasIndex(u => u.Username).IsUnique().HasFilter("[Username] IS NOT NULL");
                b.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
                b.Property(u => u.CreatedAt).IsRequired();
            });

            modelBuilder.Entity<Session>(b =>
            {
                b.ToTable("Sessions");
                b.HasKey(s => s.Id);
                b.HasIndex(s => s.UserId);
                b.Property(s => s.IssuedAt).IsRequired();
                b.Property(s => s.LastActivityAt).IsRequired();
                b.Property(s => s.IsValid).IsRequired().HasDefaultValue(true);
                b.Property(s => s.ExpiresAt).IsRequired();
                b.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PasswordResetToken>(b =>
            {
                b.ToTable("PasswordResetTokens");
                b.HasKey(t => t.Id);
                b.HasIndex(t => t.UserId);
                b.Property(t => t.TokenHash).HasMaxLength(512).IsRequired();
                b.HasIndex(t => t.TokenHash).IsUnique();
                b.Property(t => t.ExpiresAt).IsRequired();
                b.Property(t => t.CreatedAt).IsRequired();
                b.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
