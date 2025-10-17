using Azure;
using Azure.Data.Tables;
using PetAdoption.Data.TableStorage.Enums;
using PetAdoption.Data.TableStorage.Validation;
using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Data.TableStorage
{
    public class User : ITableEntity
    {
        // ITableEntity required properties
        [Required(ErrorMessage = "User name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "User name must be between 3 and 50 characters.")]
        public string PartitionKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "User email is required.")]
        [EmailAddress(ErrorMessage = "User email must be a valid email address.")]
        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Phone number must be in a valid format.")]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Address must be between 1 and 500 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [ValidIcaoCountry]
        public string Country { get; set; } = string.Empty;

        [Url(ErrorMessage = "Website must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Website URL cannot exceed 256 characters.")]
        public string? Website { get; set; }

        [Url(ErrorMessage = "Facebook URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Facebook URL cannot exceed 256 characters.")]
        public string? Facebook { get; set; }

        [Url(ErrorMessage = "Instagram URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Instagram URL cannot exceed 256 characters.")]
        public string? Instagram { get; set; }

        public bool AccountDisabled { get; set; }

        [Required(ErrorMessage = "User role is required.")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "Invalid user role.")]
        public UserRole Role { get; set; }

        [StringLength(50, ErrorMessage = "Shelter name cannot exceed 50 characters.")]
        public string? ShelterName { get; set; }

        [StringLength(50, ErrorMessage = "Shelter location cannot exceed 50 characters.")]
        public string? ShelterLocation { get; set; }

        public bool ProfileCompleted { get; set; }

        // Parameterless constructor for deserialization
        public User() { }

        public User(string userName, string userEmail, string phoneNumber, string address, string country, string? website,
            string? facebook, string? instagram, UserRole role, string? shelterName, string? shelterLocation)
        {
            PartitionKey = userName;
            RowKey = userEmail;
            PhoneNumber = phoneNumber;
            Address = address;
            Country = country;
            Website = website;
            Facebook = facebook;
            Instagram = instagram;
            AccountDisabled = false;
            Role = role;
            ShelterName = shelterName;
            ShelterLocation = shelterLocation;
            ProfileCompleted = false;
        }
    }
}