using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace TinyTickets.Api.Services
{
    public class SasReadService
    {
        private readonly BlobServiceClient _blobService;

        public SasReadService(IConfiguration config)
        {
            var cs = config["StorageAccountConnectionString"]
                     ?? throw new Exception("StorageAccountConnectionString missing");

            _blobService = new BlobServiceClient(cs);
        }

        public string GetReadUrl(string containerName, string blobName)
        {
            var container = _blobService.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            var sas = blob.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
            return sas.ToString();
        }
    }

}
