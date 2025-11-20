namespace TinyTickets.Functions.Data.Entities
{
    public class BlobAudit
    {
        public int Id { get; set; }
        public string BlobName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Container { get; set; } = string.Empty;
        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;
    }
}