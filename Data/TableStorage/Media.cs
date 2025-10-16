using Azure;
using Azure.Data.Tables;
using Data.TableStorage.Validation;
using System.ComponentModel.DataAnnotations;

namespace Data.TableStorage
{
    public class Media : ITableEntity
    {
        // ITableEntity required properties
        [Required(ErrorMessage = "Pet name is required.")]
        public string PartitionKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pet birth date is required.")]
        [ValidBirthDate]
        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        [Url(ErrorMessage = "Image 1 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image1Url { get; set; }

        [Url(ErrorMessage = "Image 2 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image2Url { get; set; }

        [Url(ErrorMessage = "Image 3 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image3Url { get; set; }

        [Url(ErrorMessage = "Image 4 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image4Url { get; set; }

        [Url(ErrorMessage = "Image 5 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image5Url { get; set; }

        [Url(ErrorMessage = "Image 6 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image6Url { get; set; }

        [Url(ErrorMessage = "Image 7 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image7Url { get; set; }

        [Url(ErrorMessage = "Image 8 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image8Url { get; set; }

        [Url(ErrorMessage = "Video 1 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Video URL cannot exceed 256 characters.")]
        public string? Video1Url { get; set; }

        // Parameterless constructor for deserialization
        public Media() { }

        public Media(string petName, DateTime birthDate)
        {
            PartitionKey = petName;
            RowKey = birthDate.ToString("yyyy-MM-dd");
        }
    }
}