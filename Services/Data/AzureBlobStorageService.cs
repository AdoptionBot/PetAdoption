using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using PetAdoption.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Implementation of Azure Blob Storage service for pet images and videos
    /// Uses a shared BlobServiceClient via factory for optimal performance
    /// </summary>
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly ILogger<AzureBlobStorageService> _logger;

        /// <summary>
        /// Gets the container name this service is configured for
        /// </summary>
        public string ContainerName => _containerName;

        /// <summary>
        /// Constructor using client factory (recommended)
        /// </summary>
        public AzureBlobStorageService(
            AzureBlobStorageClientFactory clientFactory,
            string containerName,
            ILogger<AzureBlobStorageService> logger)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            ArgumentNullException.ThrowIfNull(logger);
            
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
            }

            _blobServiceClient = clientFactory.GetClient();
            _containerName = containerName;
            _logger = logger;
            
            _logger.LogDebug("AzureBlobStorageService initialized for container: {Container}", containerName);
        }

        /// <inheritdoc/>
        public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType)
        {
            ArgumentNullException.ThrowIfNull(imageStream);
            
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException("Content type cannot be null or empty", nameof(contentType));
            }

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Ensure container exists with public blob access
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                // Generate unique filename to prevent overwrites
                var sanitizedFileName = SanitizeFileName(fileName);
                var uniqueFileName = $"{Guid.NewGuid():N}_{sanitizedFileName}";
                var blobClient = containerClient.GetBlobClient(uniqueFileName);

                // Set content type for proper browser handling
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    CacheControl = "public, max-age=31536000" // Cache for 1 year
                };

                var streamLength = imageStream.CanSeek ? imageStream.Length : -1;
                _logger.LogInformation(
                    "Uploading blob: {FileName} (ContentType: {ContentType}, Size: {Size} bytes) to container: {Container}",
                    uniqueFileName,
                    contentType,
                    streamLength,
                    _containerName);

                var startTime = DateTimeOffset.UtcNow;

                // Upload with optimized transfer options
                await blobClient.UploadAsync(imageStream, new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders,
                    TransferOptions = new Azure.Storage.StorageTransferOptions
                    {
                        InitialTransferSize = 4 * 1024 * 1024,  // 4MB initial
                        MaximumTransferSize = 4 * 1024 * 1024,  // 4MB blocks
                        MaximumConcurrency = 4                   // Parallel uploads
                    }
                });

                _logger.LogInformation("Successfully uploaded blob: {FileName} to container: {Container}", uniqueFileName, _containerName);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image: {FileName} to container: {Container}", fileName, _containerName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new ArgumentException("Image URL cannot be null or empty", nameof(imageUrl));
            }

            try
            {
                if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
                {
                    throw new ArgumentException("Invalid URL format", nameof(imageUrl));
                }

                var blobName = uri.Segments[^1];
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var deleted = await blobClient.DeleteIfExistsAsync();
                
                if (deleted.Value)
                {
                    _logger.LogInformation("Deleted blob: {BlobName} from container: {Container}", blobName, _containerName);
                }
                else
                {
                    _logger.LogWarning("Blob not found for deletion: {BlobName} in container: {Container}", blobName, _containerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image: {ImageUrl} from container: {Container}", imageUrl, _containerName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetSasUrlAsync(string imageUrl, int expiryMinutes = 60)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new ArgumentException("Image URL cannot be null or empty", nameof(imageUrl));
            }

            if (expiryMinutes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expiryMinutes), "Expiry minutes must be greater than 0");
            }

            try
            {
                if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
                {
                    throw new ArgumentException("Invalid URL format", nameof(imageUrl));
                }

                var blobName = uri.Segments[^1];
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                if (!blobClient.CanGenerateSasUri)
                {
                    _logger.LogWarning("Cannot generate SAS URL for: {ImageUrl}. Returning original URL.", imageUrl);
                    return imageUrl;
                }

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerName,
                    BlobName = blobName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow 5 min clock skew
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                
                return sasUri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate SAS URL for: {ImageUrl}", imageUrl);
                throw;
            }
        }

        /// <summary>
        /// Sanitizes a filename by removing invalid characters and path information
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            // Remove path information
            fileName = Path.GetFileName(fileName);
            
            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Limit length to avoid issues
            return sanitized.Length > 100 ? sanitized[..100] : sanitized;
        }
    }
}