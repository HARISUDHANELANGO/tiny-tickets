using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace TinyTickets.Api.Services
{
    public class SasTokenService
    {
        private readonly string _connectionString;

        public SasTokenService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("StorageAccount")
                ?? throw new Exception("Storage connection string missing");
        }

        public string GenerateUploadSas(string container, string fileName)
        {
            var containerClient = new BlobContainerClient(_connectionString, container);
            var blobClient = containerClient.GetBlobClient(fileName);

            var sas = blobClient.GenerateSasUri(
                BlobSasPermissions.Write | BlobSasPermissions.Create,
                DateTime.UtcNow.AddMinutes(20)
            );

            return sas.ToString();
        }

        public string GenerateReadSas(string container, string fileName)
        {
            var containerClient = new BlobContainerClient(_connectionString, container);
            var blobClient = containerClient.GetBlobClient(fileName);

            var sas = blobClient.GenerateSasUri(
                BlobSasPermissions.Read,
                DateTime.UtcNow.AddMinutes(20)
            );

            return sas.ToString();
        }
    }
}
