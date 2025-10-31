using Azure;
using Azure.Data.Tables;
using PetAdoption.Data.TableStorage.Validation;
using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Data.TableStorage
{
    public class Veterinary : ITableEntity
    {
        // ITableEntity required properties
        [Required(ErrorMessage = "Veterinary name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Veterinary name must be between 3 and 100 characters.")]
        public string PartitionKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "City/town is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "City/town must be between 2 and 50 characters.")]
        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        [Required(ErrorMessage = "Google Maps location URL is required.")]
        [Url(ErrorMessage = "Google Maps location must be a valid URL.")]
        [StringLength(500, ErrorMessage = "Google Maps location URL cannot exceed 500 characters.")]
        public string GoogleMapsLocation { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Phone number must be in a valid format.")]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        public string? Address { get; set; }

        [ValidIcaoCountry]
        public string? Country { get; set; }

        [OptionalUrl(ErrorMessage = "Website must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Website URL cannot exceed 256 characters.")]
        public string? Website { get; set; }

        [StringLength(500, ErrorMessage = "Opening times cannot exceed 500 characters.")]
        public string? OpeningTimes { get; set; }

        // Parameterless constructor for deserialization
        public Veterinary() { }

        public Veterinary(string veterinaryName, string cityTown, string googleMapsLocation, 
            string? phoneNumber = null, string? address = null, string? country = null, 
            string? website = null, string? openingTimes = null)
        {
            PartitionKey = veterinaryName;
            RowKey = cityTown;
            GoogleMapsLocation = googleMapsLocation;
            PhoneNumber = phoneNumber;
            Address = address;
            Country = country;
            Website = website;
            OpeningTimes = openingTimes;
        }
    }
}