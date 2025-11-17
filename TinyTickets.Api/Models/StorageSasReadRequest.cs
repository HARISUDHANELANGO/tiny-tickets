namespace TinyTickets.Api.Models
{
    public class StorageSasReadRequest
    {
        public required string Container { get; set; }
        public required string FileName { get; set; }
    }
}

