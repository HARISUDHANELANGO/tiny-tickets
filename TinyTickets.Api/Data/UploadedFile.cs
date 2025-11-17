namespace TinyTickets.Api.Data
{
    public class UploadedFile
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Container { get; set; } = string.Empty;
        public DateTime UploadedOn { get; set; }
    }
}
