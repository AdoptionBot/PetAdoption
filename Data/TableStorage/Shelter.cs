using Azure;
using Azure.Data.Tables;
using Data.TableStorage.Validation;
using System.ComponentModel.DataAnnotations;

namespace Data.TableStorage
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

        [Url(ErrorMessage = "Website must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Website URL cannot exceed 256 characters.")]
        public string? Website { get; set; }

        [Url(ErrorMessage = "Facebook URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Facebook URL cannot exceed 256 characters.")]
        public string? Facebook { get; set; }

        [Url(ErrorMessage = "Instagram URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Instagram URL cannot exceed 256 characters.")]
        public string? Instagram { get; set; }

        // Parameterless constructor for deserialization
        public Shelter() { }

        public Shelter(string shelterName, string shelterCityTown, string country, string phoneNumber,
            string address, string email, string? website, string? facebook, string? instagram)
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
        }
    }
}