namespace TinyTickets.Api.Models
{
    public class FileMetadata
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string BlobName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedOn { get; set; }
    }
}
