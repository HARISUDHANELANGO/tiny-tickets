using Microsoft.EntityFrameworkCore;
using TinyTickets.Functions.Data.Entities;

namespace TinyTickets.Functions.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<TicketAudit> TicketAudits => Set<TicketAudit>();
        public DbSet<BlobAudit> BlobAudits => Set<BlobAudit>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TicketAudit>().ToTable("TicketAudit");
            modelBuilder.Entity<BlobAudit>().ToTable("BlobAudit");
        }
    }
}
