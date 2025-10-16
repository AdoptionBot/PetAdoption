using Azure;
using Azure.Data.Tables;
using PetAdoption.Data.TableStorage.Enums;
using PetAdoption.Data.TableStorage.Validation;
using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Data.TableStorage
{
    public class AdoptionApplication : ITableEntity
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
        [Required(ErrorMessage = "Pet name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Pet name must be between 3 and 50 characters.")]
        public string PetName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pet birth date is required.")]
        [ValidBirthDate]
        public DateTime PetBirthDate { get; set; }

        public DateTime DateSubmitted { get; }

        [Required(ErrorMessage = "Adoption status is required.")]
        [EnumDataType(typeof(AdoptionStatus), ErrorMessage = "Invalid adoption status.")]
        public AdoptionStatus AdoptionStatus { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
        public string? Notes { get; set; }

        // Parameterless constructor for deserialization
        public AdoptionApplication() { }

        public AdoptionApplication(
            string userName, string userEmail, string petName, DateTime petBirthDate, string? notes)
        {
            PartitionKey = userName;
            RowKey = userEmail;
            PetName = petName;
            PetBirthDate = petBirthDate;
            DateSubmitted = DateTime.Now;
            AdoptionStatus = AdoptionStatus.Submitted;
            Notes = notes;
        }
    }
}
