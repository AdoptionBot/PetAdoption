using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Service for downloading photos from Google Places API and uploading to Azure Blob Storage
    /// </summary>
    public class GooglePlacesPhotoService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GooglePlacesPhotoService> _logger;
        private readonly string _googleApiKey;

        public GooglePlacesPhotoService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GooglePlacesPhotoService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _googleApiKey = configuration["GoogleApiKey"] ?? string.Empty;
        }

        /// <summary>
        /// Downloads a photo from Google Places and uploads it to Azure Blob Storage
        /// </summary>
        /// <param name="photoReference">Google Places photo reference</param>
        /// <param name="entityName">Name of the entity (for filename)</param>
        /// <param name="blobStorageService">The blob storage service to use for upload</param>
        /// <param name="maxWidth">Maximum width of the photo (default 800px)</param>
        /// <returns>Azure Blob Storage URL of the uploaded photo, or null if failed</returns>
        public async Task<string?> DownloadAndUploadPhotoAsync(
            string? photoReference, 
            string entityName, 
            IAzureBlobStorageService blobStorageService,
            int maxWidth = 800)
        {
            if (string.IsNullOrEmpty(photoReference) || string.IsNullOrEmpty(_googleApiKey))
            {
                _logger.LogWarning("Photo reference or Google API key is missing");
                return null;
            }

            try
            {
                _logger.LogInformation("Downloading photo from Google Places for: {EntityName}", entityName);

                // Download photo from Google Places API
                var photoUrl = $"https://maps.googleapis.com/maps/api/place/photo?maxwidth={maxWidth}&photo_reference={photoReference}&key={_googleApiKey}";
                
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(photoUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download photo from Google Places. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                // Read photo content
                var photoBytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

                _logger.LogInformation("Downloaded photo: {Size} bytes, ContentType: {ContentType}", photoBytes.Length, contentType);

                // Generate filename
                var extension = GetExtensionFromContentType(contentType);
                var fileName = $"{SanitizeFileName(entityName)}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";

                // Upload to Azure Blob Storage
                using var stream = new MemoryStream(photoBytes);
                var blobUrl = await blobStorageService.UploadImageAsync(stream, fileName, contentType);

                _logger.LogInformation("Successfully uploaded photo to blob storage: {BlobUrl}", blobUrl);

                return blobUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download and upload photo for: {EntityName}", entityName);
                return null;
            }
        }

        private string GetExtensionFromContentType(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" or "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg"
            };
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Limit length
            return sanitized.Length > 50 ? sanitized[..50] : sanitized;
        }
    }
}