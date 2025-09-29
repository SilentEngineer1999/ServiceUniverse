using Microsoft.EntityFrameworkCore;
using PatientService.Models;

namespace PatientService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Patient> Patients => Set<Patient>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.CitizenId)
                .IsUnique()
                .HasFilter("\"CitizenId\" IS NOT NULL");
        }
    }
}