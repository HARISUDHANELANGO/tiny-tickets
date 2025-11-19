using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace TinyTickets.Api.Services
{
    public class SasTokenService
    {
        private readonly BlobServiceClient _blobService;

        public SasTokenService(IConfiguration config)
        {
            var cs = config["StorageAccountConnectionString"]
                     ?? throw new Exception("StorageAccountConnectionString missing");

            _blobService = new BlobServiceClient(cs);
        }

        public string GenerateUploadSas(string containerName, string blobName)
        {
            var container = _blobService.GetBlobContainerClient(containerName);
            container.CreateIfNotExists();

            var blob = container.GetBlobClient(blobName);

            var sas = blob.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddMinutes(10));
            return sas.ToString();
        }
    }

}
