using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using PetAdoption.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Implementation of Azure Blob Storage service for pet images and videos
    /// Uses the same Azure Storage account as Table Storage
    /// </summary>
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly ILogger<AzureBlobStorageService>? _logger;

        /// <summary>
        /// Constructor with connection string
        /// </summary>
        public AzureBlobStorageService(string connectionString, string containerName, ILogger<AzureBlobStorageService>? logger = null)
        {
            _logger = logger;
            
            if (string.IsNullOrEmpty(connectionString))
            {
                var error = "Connection string cannot be null or empty";
                _logger?.LogError(error);
                throw new ArgumentNullException(nameof(connectionString), error);
            }

            if (string.IsNullOrEmpty(containerName))
            {
                var error = "Container name cannot be null or empty";
                _logger?.LogError(error);
                throw new ArgumentNullException(nameof(containerName), error);
            }

            try
            {
                _logger?.LogInformation("Creating BlobServiceClient with connection string (length: {Length})", connectionString.Length);
                _blobServiceClient = new BlobServiceClient(connectionString);
                _containerName = containerName;
                _logger?.LogInformation("BlobServiceClient created successfully for container: {Container}", containerName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create BlobServiceClient. Connection string length: {Length}, Container: {Container}", 
                    connectionString?.Length ?? 0, containerName);
                throw new InvalidOperationException($"Failed to initialize Azure Blob Storage service: {ex.Message}", ex);
            }
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
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

                _logger?.LogInformation("Uploading blob: {FileName} (ContentType: {ContentType})", uniqueFileName, contentType);

                // Upload the image
                await blobClient.UploadAsync(imageStream, new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                });

                _logger?.LogInformation("Successfully uploaded blob: {FileName}", uniqueFileName);

                // Return the public URL
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to upload image: {FileName}", fileName);
                throw;
            }
        }

        public async Task DeleteImageAsync(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);
                var blobName = uri.Segments.Last();
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
                _logger?.LogInformation("Deleted blob: {BlobName}", blobName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete image: {ImageUrl}", imageUrl);
                throw;
            }
        }

        public async Task<string> GetSasUrlAsync(string imageUrl, int expiryMinutes = 60)
        {
            try
            {
                var uri = new Uri(imageUrl);
                var blobName = uri.Segments.Last();
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                // Check if the blob client can generate SAS tokens
                if (!blobClient.CanGenerateSasUri)
                {
                    _logger?.LogWarning("Cannot generate SAS URL for: {ImageUrl}", imageUrl);
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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to generate SAS URL for: {ImageUrl}", imageUrl);
                throw;
            }
        }
    }
}