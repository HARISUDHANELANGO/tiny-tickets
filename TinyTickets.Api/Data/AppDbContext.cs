using Microsoft.EntityFrameworkCore;

namespace TinyTickets.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Ticket> Tickets => Set<Ticket>();
    }
}
