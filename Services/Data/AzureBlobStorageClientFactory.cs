using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Factory for creating BlobServiceClient instances
    /// Ensures a single client is reused across the application for better performance
    /// </summary>
    public class AzureBlobStorageClientFactory
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<AzureBlobStorageClientFactory> _logger;

        public AzureBlobStorageClientFactory(string connectionString, ILogger<AzureBlobStorageClientFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty");
            }

            try
            {
                _logger.LogInformation("Creating singleton BlobServiceClient");
                _blobServiceClient = new BlobServiceClient(connectionString);
                _logger.LogInformation("BlobServiceClient created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to create BlobServiceClient");
                throw new InvalidOperationException($"Failed to initialize Azure Blob Storage client factory: {ex.Message}", ex);
            }
        }

        public BlobServiceClient GetClient() => _blobServiceClient;
    }
}