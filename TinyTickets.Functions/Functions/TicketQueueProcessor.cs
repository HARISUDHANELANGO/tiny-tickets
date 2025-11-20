using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TinyTickets.Functions.Models;
using TinyTickets.Functions.Services;

namespace TinyTickets.Functions.Functions
{
    public class TicketQueueProcessor
    {
        private readonly ITicketProcessor _processor;
        private readonly ILogger<TicketQueueProcessor> _logger;

        public TicketQueueProcessor(ITicketProcessor processor, ILogger<TicketQueueProcessor> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        [Function("TicketQueueProcessor")]
        public async Task Run(
            [ServiceBusTrigger("ticket-events", Connection = "SERVICE_BUS_CONNECTION")] string message)
        {
            _logger.LogInformation("Received Service Bus message: {Message}", message);

            TicketMessage? ticket;
            try
            {
                ticket = JsonSerializer.Deserialize<TicketMessage>(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize Service Bus message.");
                return;
            }

            if (ticket == null)
            {
                _logger.LogWarning("Received empty or invalid ticket message.");
                return;
            }

            await _processor.ProcessAsync(ticket);

            _logger.LogInformation("TicketQueueProcessor completed for TicketId={Id}", ticket.Id);
        }
    }
}
