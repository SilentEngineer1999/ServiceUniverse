using HealthApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthApi.Data
{
    public static class SeedDb
    {
        public static async Task EnsureCreatedAndSeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HealthDbContext>();

            // If you prefer migrations, replace EnsureCreated with Migrate and add migrations to the project.
            await db.Database.EnsureCreatedAsync();

            if (!await db.Doctors.AnyAsync())
            {
                db.Doctors.AddRange(
                    new Doctor { Id = "d1", Name = "Dr. Ayesha Rahman", Specialty = "General Practice",
                        Slots = new List<string> { "2025-10-16T09:00","2025-10-16T09:30","2025-10-16T10:00" } },
                    new Doctor { Id = "d2", Name = "Dr. Samuel Lee", Specialty = "Cardiology",
                        Slots = new List<string> { "2025-10-16T11:00","2025-10-16T11:30","2025-10-16T12:00" } },
                    new Doctor { Id = "d3", Name = "Dr. Priya Nair", Specialty = "Dermatology",
                        Slots = new List<string> { "2025-10-16T13:00","2025-10-16T13:30","2025-10-16T14:00" } },
                    new Doctor { Id = "d4", Name = "Dr. Miguel Santos", Specialty = "Pediatrics",
                        Slots = new List<string> { "2025-10-16T09:15","2025-10-16T09:45","2025-10-16T10:15" } },
                    new Doctor { Id = "d5", Name = "Dr. Olivia Chen", Specialty = "Orthopedics",
                        Slots = new List<string> { "2025-10-16T15:00","2025-10-16T15:30","2025-10-16T16:00" } }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.Appointments.AnyAsync())
            {
                db.Appointments.Add(new Appointment
                {
                    Id = "a1",
                    PatientName = "Maria H.",
                    DoctorId = "d1",
                    Time = "2025-10-16T09:00"
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
