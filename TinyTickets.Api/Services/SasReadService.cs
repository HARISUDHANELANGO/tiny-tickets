using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace TinyTickets.Api.Services
{
    public class SasReadService
    {
        private readonly string _connectionString;

        public SasReadService(IConfiguration config)
        {
            _connectionString =
                config.GetConnectionString("StorageAccount")
                ?? Environment.GetEnvironmentVariable("StorageAccount__ConnectionString")
                ?? throw new Exception("Storage connection string missing");
        }

        /// <summary>
        /// Get a short-lived SAS read URL for any blob.
        /// </summary>
        public string GetReadUrl(string containerName, string blobName)
        {
            var blobService = new BlobServiceClient(_connectionString);
            var container = blobService.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            var sas = blob.GenerateSasUri(
                BlobSasPermissions.Read,
                DateTimeOffset.UtcNow.AddMinutes(10)
            );

            return sas.ToString();
        }
    }
}
