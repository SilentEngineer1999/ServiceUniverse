using System.Data.Common;
using PassBuy.Models;

namespace PassBuy.Data
{
    public static class DbSeeder
    {
        public static void SeedEducationProviders(AppDbContext db)
        {
            if (db.EducationProviders.Any())
            {
                Console.WriteLine("Education providers already exist, skipping seed.");
                return;
            }

            Console.WriteLine("Seeding default education providers...");

            var defaultProviders = new List<EducationProvider>
            {
                new() { Id = Guid.NewGuid(), EduCode = "UOW", Name = "University of Wollongong" },
                new() { Id = Guid.NewGuid(), EduCode = "USYD", Name = "University of Sydney" },
                new() { Id = Guid.NewGuid(), EduCode = "WSU", Name = "Western Sydney University" },
                new() { Id = Guid.NewGuid(), EduCode = "UNSW", Name = "University of New South Wales" },
                new() { Id = Guid.NewGuid(), EduCode = "UTS", Name = "University of Technology Sydney" },
                new() { Id = Guid.NewGuid(), EduCode = "TAFENSW", Name = "TAFE NSW" },
                new() { Id = Guid.NewGuid(), EduCode = "ANU", Name = "Australian National University" },
                new() { Id = Guid.NewGuid(), EduCode = "UA", Name = "University of Adelaide" },
                new() { Id = Guid.NewGuid(), EduCode = "UWA", Name = "University of Western Australia" },
                new() { Id = Guid.NewGuid(), EduCode = "CDU", Name = "Charles Darwin University" },
                new() { Id = Guid.NewGuid(), EduCode = "UQ", Name = "University of Queensland" },
                new() { Id = Guid.NewGuid(), EduCode = "QUT", Name = "Queensland University of Technology" },
            };

            db.EducationProviders.AddRange(defaultProviders);
            db.SaveChanges();

            Console.WriteLine("Default education providers seeded.");
        }

        public static void SeedTransportEmployers(AppDbContext db)
        {
            if (db.TransportEmployers.Any())
            {
                Console.WriteLine("Transport Employers already exist, skipping seed.");
                return;
            }

            Console.WriteLine("Seeding default transport employers...");

            var defaultEmployers = new List<TransportEmployer>
            {
                new() { Id = Guid.NewGuid(), Name = "State Buses" },
                new() { Id = Guid.NewGuid(), Name = "State Trains" },
                new() { Id = Guid.NewGuid(), Name = "State Ferries" },
                new() { Id = Guid.NewGuid(), Name = "Transit Systems" },
                new() { Id = Guid.NewGuid(), Name = "BusWays" },
                new() { Id = Guid.NewGuid(), Name = "Veolia" },
                new() { Id = Guid.NewGuid(), Name = "MTR Corp" }
            };
        }
    }
}
