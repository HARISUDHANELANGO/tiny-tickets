using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TinyTickets.Functions.Data;
using TinyTickets.Functions.Data.Entities;
using TinyTickets.Functions.Models;

namespace TinyTickets.Functions.Services
{
    public class BlobProcessor : IBlobProcessor
    {
        private readonly AppDbContext _db;
        private readonly ILogger<BlobProcessor> _logger;

        public BlobProcessor(AppDbContext db, ILogger<BlobProcessor> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task ProcessAsync(BlobInfo blobInfo)
        {
            _logger.LogInformation("Processing blob upload: {@BlobInfo}", blobInfo);

            var audit = new BlobAudit
            {
                BlobName = blobInfo.BlobName,
                FileName = blobInfo.FileName,
                Size = blobInfo.Size,
                Container = blobInfo.Container,
                UploadedOn = blobInfo.UploadedOn
            };

            await _db.BlobAudits.AddAsync(audit);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Blob audit saved successfully for Blob={Blob}", blobInfo.BlobName);
        }
    }
}
