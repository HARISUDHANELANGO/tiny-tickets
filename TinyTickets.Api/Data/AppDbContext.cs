using Microsoft.EntityFrameworkCore;
using TinyTickets.Api.Models;

namespace TinyTickets.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    }
}
