namespace TinyTickets.Api.Data
{
    public class UploadedFile
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;     // Original file name
        public string BlobName { get; set; } = string.Empty;      // GUID_filename
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }                            // File size in bytes
        public string Container { get; set; } = string.Empty;     // e.g., ticket-images
        public DateTime UploadedOn { get; set; }
    }
}
