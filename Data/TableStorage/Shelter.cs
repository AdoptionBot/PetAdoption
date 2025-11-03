using Azure;
using Azure.Data.Tables;
using PetAdoption.Data.TableStorage.Validation;
using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Data.TableStorage
{
    public class Shelter : ITableEntity
    {
        // ITableEntity required properties
        [Required(ErrorMessage = "Shelter name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Shelter name must be between 3 and 50 characters.")]
        public string PartitionKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shelter city/town is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Shelter city/town must be between 3 and 50 characters.")]
        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        [Required(ErrorMessage = "Country is required.")]
        [ValidIcaoCountry]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Phone number must be in a valid format.")]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Address must be between 1 and 500 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [OptionalUrl(ErrorMessage = "Website must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Website URL cannot exceed 256 characters.")]
        public string? Website { get; set; }

        [OptionalUrl(ErrorMessage = "Facebook URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Facebook URL cannot exceed 256 characters.")]
        public string? Facebook { get; set; }

        [OptionalUrl(ErrorMessage = "Instagram URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Instagram URL cannot exceed 256 characters.")]
        public string? Instagram { get; set; }

        [OptionalUrl(ErrorMessage = "X URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "X URL cannot exceed 256 characters.")]
        public string? X { get; set; }

        // Google Maps Location - NEW
        [OptionalUrl(ErrorMessage = "Google Maps location must be a valid URL.")]
        [StringLength(500, ErrorMessage = "Google Maps location URL cannot exceed 500 characters.")]
        public string? GoogleMapsLocation { get; set; }

        // Opening Times - NEW
        [StringLength(500, ErrorMessage = "Opening times cannot exceed 500 characters.")]
        public string? OpeningTimes { get; set; }

        // Adoption Agreement Properties
        [StringLength(2000, ErrorMessage = "Adoption agreement rules cannot exceed 2000 characters.")]
        public string? AdoptionAgreementRules { get; set; }

        // Document 1
        [StringLength(100, ErrorMessage = "Document name cannot exceed 100 characters.")]
        public string? Document1Name { get; set; }

        [OptionalUrl(ErrorMessage = "Document 1 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Document URL cannot exceed 256 characters.")]
        public string? Document1Url { get; set; }

        // Document 2
        [StringLength(100, ErrorMessage = "Document name cannot exceed 100 characters.")]
        public string? Document2Name { get; set; }

        [OptionalUrl(ErrorMessage = "Document 2 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Document URL cannot exceed 256 characters.")]
        public string? Document2Url { get; set; }

        // Document 3
        [StringLength(100, ErrorMessage = "Document name cannot exceed 100 characters.")]
        public string? Document3Name { get; set; }

        [OptionalUrl(ErrorMessage = "Document 3 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Document URL cannot exceed 256 characters.")]
        public string? Document3Url { get; set; }

        // Document 4
        [StringLength(100, ErrorMessage = "Document name cannot exceed 100 characters.")]
        public string? Document4Name { get; set; }

        [OptionalUrl(ErrorMessage = "Document 4 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Document URL cannot exceed 256 characters.")]
        public string? Document4Url { get; set; }

        // Parameterless constructor for deserialization
        public Shelter() { }

        public Shelter(string shelterName, string shelterCityTown, string country, string phoneNumber,
            string address, string email, string? website, string? facebook, string? instagram, string? x,
            string? googleMapsLocation = null, string? openingTimes = null)
        {
            PartitionKey = shelterName;
            RowKey = shelterCityTown;
            Country = country;
            PhoneNumber = phoneNumber;
            Address = address;
            Email = email;
            Website = website;
            Facebook = facebook;
            Instagram = instagram;
            X = x;
            GoogleMapsLocation = googleMapsLocation;
            OpeningTimes = openingTimes;
        }
    }
}