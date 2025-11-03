namespace PetAdoption.Services.Interfaces
{
    /// <summary>
    /// Service for managing image uploads to Azure Blob Storage
    /// </summary>
    public interface IAzureBlobStorageService
    {
        /// <summary>
        /// Gets the container name this service is configured for
        /// </summary>
        string ContainerName { get; }

        /// <summary>
        /// Uploads an image to blob storage and returns the public URL
        /// </summary>
        /// <param name="imageStream">The image stream to upload</param>
        /// <param name="fileName">The desired file name</param>
        /// <param name="contentType">The content type (e.g., image/jpeg)</param>
        /// <returns>The public URL of the uploaded image</returns>
        Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType);

        /// <summary>
        /// Deletes an image from blob storage
        /// </summary>
        /// <param name="imageUrl">The URL of the image to delete</param>
        Task DeleteImageAsync(string imageUrl);

        /// <summary>
        /// Gets a SAS URL for temporary secure access
        /// </summary>
        /// <param name="imageUrl">The blob URL</param>
        /// <param name="expiryMinutes">Minutes until expiry</param>
        /// <returns>URL with SAS token</returns>
        Task<string> GetSasUrlAsync(string imageUrl, int expiryMinutes = 60);
    }
}