namespace TinyTickets.Functions.Data.Entities
{
    public class TicketAudit
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime ReceivedOn { get; set; } = DateTime.UtcNow;
    }
}