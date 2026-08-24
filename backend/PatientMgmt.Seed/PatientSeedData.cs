using Microsoft.EntityFrameworkCore;
using PatientMgmt.DataAccess;
using PatientMgmt.Domain.Entities;

namespace PatientMgmt.Seed
{
    /// <summary>
    /// Sample patient records for downstream module development/testing (Module 2 plan §9 task #12).
    /// Safe to run repeatedly — no-ops if any patients already exist.
    /// </summary>
    public static class PatientSeedData
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.Patients.AnyAsync())
            {
                Console.WriteLine("Patients already exist. Skipping patient seed.");
                return;
            }

            var now = DateTime.UtcNow;
            var patients = new List<Patient>
            {
                new()
                {
                    Id = Guid.NewGuid(), PatientCode = "P-00001", FullName = "Alice Johnson",
                    DateOfBirth = new DateTime(1985, 3, 12), Gender = Gender.Female,
                    PhoneNumber = "5551234567", Email = "alice.johnson@example.com", Address = "12 Maple St",
                    CreatedAt = now, UpdatedAt = now
                },
                new()
                {
                    Id = Guid.NewGuid(), PatientCode = "P-00002", FullName = "Brian Smith",
                    DateOfBirth = new DateTime(1978, 11, 2), Gender = Gender.Male,
                    PhoneNumber = "5552345678", Email = null, Address = "45 Oak Ave",
                    CreatedAt = now, UpdatedAt = now
                },
                new()
                {
                    Id = Guid.NewGuid(), PatientCode = "P-00003", FullName = "Carla Mendes",
                    ApproxAgeAtEntry = 62, EntryDate = now, Gender = Gender.Female,
                    PhoneNumber = "5553456789", Email = "carla.mendes@example.com", Address = null,
                    CreatedAt = now, UpdatedAt = now
                },
                new()
                {
                    Id = Guid.NewGuid(), PatientCode = "P-00004", FullName = "David Lee",
                    DateOfBirth = new DateTime(2001, 7, 30), Gender = Gender.Male,
                    PhoneNumber = "5554567890", Email = null, Address = null,
                    CreatedAt = now, UpdatedAt = now
                },
                new()
                {
                    Id = Guid.NewGuid(), PatientCode = "P-00005", FullName = "Priya Nair",
                    DateOfBirth = new DateTime(1993, 9, 18), Gender = Gender.Other,
                    PhoneNumber = "5555678901", Email = "priya.nair@example.com", Address = "9 Birch Ln",
                    CreatedAt = now, UpdatedAt = now
                }
            };

            db.Patients.AddRange(patients);
            await db.SaveChangesAsync();
            Console.WriteLine($"Seeded {patients.Count} sample patients.");
        }
    }
}
