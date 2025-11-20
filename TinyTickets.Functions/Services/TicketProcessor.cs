using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TinyTickets.Functions.Data;
using TinyTickets.Functions.Data.Entities;
using TinyTickets.Functions.Models;

namespace TinyTickets.Functions.Services
{
    public class TicketProcessor : ITicketProcessor
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TicketProcessor> _logger;

        public TicketProcessor(AppDbContext db, ILogger<TicketProcessor> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task ProcessAsync(TicketMessage message)
        {
            _logger.LogInformation("Processing ticket message: {@Message}", message);

            var audit = new TicketAudit
            {
                TicketId = message.Id,
                Title = message.Title,
                ReceivedOn = DateTime.UtcNow
            };

            await _db.TicketAudits.AddAsync(audit);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Ticket audit saved successfully for TicketId={Id}", message.Id);
        }
    }
}
