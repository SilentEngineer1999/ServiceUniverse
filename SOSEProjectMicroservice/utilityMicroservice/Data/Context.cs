using backendServices.Models;
using Microsoft.EntityFrameworkCore;

namespace backendServices.Data
{
    public class UtilityDbContext : DbContext
    {
        public UtilityDbContext(DbContextOptions<UtilityDbContext> options) : base(options) { }

        public DbSet<Utility> Utilities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Utility>().ToTable("utility");
            modelBuilder.Entity<Utility>().HasKey(u => u.UtilityId);
        }
    }
}
