using Microsoft.EntityFrameworkCore;
using BookingService.Models;

namespace BookingService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Booking> Bookings => Set<Booking>();
    }
}