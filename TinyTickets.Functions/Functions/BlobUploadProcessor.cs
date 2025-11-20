using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using TinyTickets.Functions.Models;
using TinyTickets.Functions.Services;

namespace TinyTickets.Functions.Functions
{
    public class BlobUploadProcessor
    {
        private readonly IBlobProcessor _processor;
        private readonly ILogger<BlobUploadProcessor> _logger;

        public BlobUploadProcessor(IBlobProcessor processor, ILogger<BlobUploadProcessor> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        [Function("BlobUploadProcessor")]
        public async Task Run(
            [BlobTrigger("ticket-images/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
            string name,
            FunctionContext context)
        {
            _logger.LogInformation("Blob created: {Name}", name);

            // Access triggering metadata
            var binding = context.BindingContext.BindingData;
            var container = binding["ContainerName"]?.ToString() ?? "ticket-images";

            var blobInfo = new BlobInfo
            {
                BlobName = name,
                FileName = ExtractOriginalFileName(name),
                Size = blobStream.Length,
                Container = container,
                UploadedOn = DateTime.UtcNow
            };

            await _processor.ProcessAsync(blobInfo);

            _logger.LogInformation("BlobUploadProcessor completed for Blob={Blob}", name);
        }

        private string ExtractOriginalFileName(string blobName)
        {
            // blobName looks like: {guid}_originalname.ext
            var index = blobName.IndexOf('_');
            return index >= 0 ? blobName[(index + 1)..] : blobName;
        }
    }
}
