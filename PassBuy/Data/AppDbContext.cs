using Microsoft.EntityFrameworkCore;
using PassBuy.Models;

namespace PassBuy.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PassBuyCard> PassBuyCards => Set<PassBuyCard>();
    public DbSet<PassBuyCardApplication> PassBuyCardApplications => Set<PassBuyCardApplication>();
    public DbSet<EducationProvider> EducationProviders => Set<EducationProvider>();
    public DbSet<EducationDetails> EducationDetails => Set<EducationDetails>();
    public DbSet<TransportEmployer> TransportEmployers => Set<TransportEmployer>();
    public DbSet<TransportEmploymentDetails> TransportEmploymentDetails => Set<TransportEmploymentDetails>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        // Foreign Key constraints
        model.Entity<PassBuyCardApplication>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<EducationDetails>()
            .HasOne<PassBuyCardApplication>()
            .WithOne(a => a.EducationDetails)
            .HasForeignKey<EducationDetails>(d => d.ApplicationId)
            .IsRequired(false);

        model.Entity<TransportEmploymentDetails>()
            .HasOne<PassBuyCardApplication>()
            .WithOne(a => a.TransportEmploymentDetails)
            .HasForeignKey<TransportEmploymentDetails>(t => t.ApplicationId)
            .IsRequired(false);

        model.Entity<EducationDetails>()
            .HasOne<EducationProvider>()
            .WithMany()
            .HasForeignKey(d => d.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<TransportEmploymentDetails>()
            .HasOne<TransportEmployer>()
            .WithMany()
            .HasForeignKey(t => t.EmployerId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<PassBuyCard>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<PassBuyCard>()
            .HasOne(c => c.Application)
            .WithOne()
            .HasForeignKey<PassBuyCard>("ApplicationId")
            .IsRequired(false);

        // Unique constraints
        model.Entity<EducationDetails>()
            .HasIndex(e => new { e.ProviderId, e.StudentNumber })
            .IsUnique();

        model.Entity<EducationProvider>()
            .HasIndex(p => p.EduCode)
            .IsUnique();

        model.Entity<EducationProvider>()
            .HasIndex(p => p.Name)
            .IsUnique();

        model.Entity<TransportEmploymentDetails>()
            .HasIndex(t => new { t.EmployerId, t.EmployeeNumber })
            .IsUnique();

        model.Entity<TransportEmployer>()
            .HasIndex(t => t.Name)
            .IsUnique();
    }
}
