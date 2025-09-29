using Microsoft.EntityFrameworkCore;
using EnrolmentService.Models;

namespace EnrolmentService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    }
}