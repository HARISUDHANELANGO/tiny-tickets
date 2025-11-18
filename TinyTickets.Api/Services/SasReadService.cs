using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace TinyTickets.Api.Services
{
    public class SasReadService
    {
        private readonly string _conn;

        public SasReadService(IConfiguration config)
        {
            _conn =
                config.GetConnectionString("StorageAccount")
                ?? Environment.GetEnvironmentVariable("StorageAccount__ConnectionString")
                ?? throw new Exception("Storage connection string missing");
        }

        public string GetReadUrl(string container, string blobName)
        {
            var service = new BlobServiceClient(_conn);
            var blob = service.GetBlobContainerClient(container).GetBlobClient(blobName);

            var sas = blob.GenerateSasUri(
                BlobSasPermissions.Read,
                DateTimeOffset.UtcNow.AddMinutes(10));

            return sas.ToString();
        }
    }
}
