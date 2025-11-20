using TinyTickets.Functions.Models;

namespace TinyTickets.Functions.Services
{
    public interface ITicketProcessor
    {
        Task ProcessAsync(TicketMessage message);
    }
}
