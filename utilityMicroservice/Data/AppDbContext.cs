using backendServices.Models;
using Microsoft.EntityFrameworkCore;

namespace backendServices.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Force table name to lowercase "users"
            modelBuilder.Entity<User>().ToTable("users");
        }
    }
}
