using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace TinyTickets.Api.Services
{
    public class SasTokenService
    {
        private readonly string _conn;

        public SasTokenService(IConfiguration config)
        {
            _conn =
                config.GetConnectionString("StorageAccount")
                ?? Environment.GetEnvironmentVariable("StorageAccount__ConnectionString")
                ?? throw new Exception("Storage connection string missing");
        }

        public string GenerateUploadSas(string containerName, string blobName)
        {
            var service = new BlobServiceClient(_conn);
            var container = service.GetBlobContainerClient(containerName);
            container.CreateIfNotExists();

            var blob = container.GetBlobClient(blobName);

            var sas = blob.GenerateSasUri(
                BlobSasPermissions.Create | BlobSasPermissions.Write,
                DateTimeOffset.UtcNow.AddMinutes(15));

            return sas.ToString();
        }
    }
}
