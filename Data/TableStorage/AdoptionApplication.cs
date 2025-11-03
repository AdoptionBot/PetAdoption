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
        // PartitionKey: Date when application was created (yyyy-MM-dd) for efficient querying by date
        public string PartitionKey { get; set; } = string.Empty;

        // RowKey: Unique timestamp when application was created (yyyy-MM-ddTHH:mm:ss.fffffffZ)
        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties - These are the actual data fields
        [Required(ErrorMessage = "User email is required.")]
        [EmailAddress(ErrorMessage = "User email must be a valid email address.")]
        public string UserEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "User name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "User name must be between 3 and 50 characters.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pet name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Pet name must be between 3 and 50 characters.")]
        public string PetName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pet birth date is required.")]
        public string PetBirthDateString { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pet birth date is required.")]
        [ValidBirthDate]
        public DateTime PetBirthDate { get; set; }

        public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;

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
            var now = DateTime.UtcNow;
            
            // Set PartitionKey to date (for efficient date-based queries)
            PartitionKey = now.ToString("yyyy-MM-dd");
            
            // Set RowKey to full timestamp with ticks for uniqueness
            RowKey = now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

            // Set actual data properties
            UserEmail = userEmail;
            UserName = userName;
            PetName = petName;
            PetBirthDate = DateTime.SpecifyKind(petBirthDate, DateTimeKind.Utc);
            PetBirthDateString = petBirthDate.ToString("yyyy-MM-dd");
            DateSubmitted = now;
            AdoptionStatus = AdoptionStatus.Submitted;
            Notes = notes;
        }
    }
}
