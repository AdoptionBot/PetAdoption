using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Implementation of Azure Blob Storage service for pet images and videos
    /// Uses the same Azure Storage account as Table Storage
    /// </summary>
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly KeyVaultSecretService? _keyVaultService;
        private readonly IConfiguration? _configuration;
        private BlobServiceClient? _blobServiceClient;
        private string? _containerName;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        /// <summary>
        /// Constructor for direct connection string usage (for testing)
        /// </summary>
        public AzureBlobStorageService(string connectionString, string containerName = "pet-media")
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerName = containerName;
        }

        /// <summary>
        /// Constructor with Key Vault integration (for production)
        /// Uses the same storage account connection string as Table Storage
        /// </summary>
        public AzureBlobStorageService(KeyVaultSecretService keyVaultService, IConfiguration configuration)
        {
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Ensures the Blob Service Client is initialized
        /// </summary>
        private async Task EnsureBlobServiceClientAsync()
        {
            if (_blobServiceClient != null) return;

            await _initLock.WaitAsync();
            try
            {
                if (_blobServiceClient == null && _keyVaultService != null && _configuration != null)
                {
                    // Retrieve the shared storage connection string from Key Vault
                    var connectionString = await _keyVaultService.GetStorageConnectionStringAsync();
                    _blobServiceClient = new BlobServiceClient(connectionString);
                    _containerName = _configuration["Azure:BlobStorage:ContainerName"] ?? "pet-media";
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType)
        {
            await EnsureBlobServiceClientAsync();

            var containerClient = _blobServiceClient!.GetBlobContainerClient(_containerName!);
            
            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Generate unique filename to prevent overwrites
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            // Set content type for proper browser handling
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            // Upload the image
            await blobClient.UploadAsync(imageStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            // Return the public URL
            return blobClient.Uri.ToString();
        }

        public async Task DeleteImageAsync(string imageUrl)
        {
            await EnsureBlobServiceClientAsync();

            var uri = new Uri(imageUrl);
            var blobName = uri.Segments.Last();
            
            var containerClient = _blobServiceClient!.GetBlobContainerClient(_containerName!);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<string> GetSasUrlAsync(string imageUrl, int expiryMinutes = 60)
        {
            await EnsureBlobServiceClientAsync();

            var uri = new Uri(imageUrl);
            var blobName = uri.Segments.Last();
            
            var containerClient = _blobServiceClient!.GetBlobContainerClient(_containerName!);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if the blob client can generate SAS tokens
            if (!blobClient.CanGenerateSasUri)
            {
                return imageUrl; // Return original URL if SAS generation not available
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }
    }
}