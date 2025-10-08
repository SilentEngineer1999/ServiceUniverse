using Microsoft.EntityFrameworkCore;
using PassBuy.Models;   // adjust if your models namespace differs

namespace PassBuy.Data   // <- ensure this matches your project/usage
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("uuid-ossp");

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(u => u.AuthToken);

                entity.Property(u => u.AuthToken)
                      .HasColumnName("auth_token")
                      .HasDefaultValueSql("uuid_generate_v4()")
                      .ValueGeneratedOnAdd();

                entity.Property(u => u.FirstName).HasColumnName("firstName").IsRequired();
                entity.Property(u => u.LastName).HasColumnName("lastName").IsRequired();
                entity.Property(u => u.Password).HasColumnName("password").IsRequired();
                entity.Property(u => u.Email).HasColumnName("email").IsRequired();

                entity.HasIndex(u => u.Email).IsUnique();
            });
        }
    }
}