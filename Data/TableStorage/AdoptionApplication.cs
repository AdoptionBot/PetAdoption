using Azure;
using Azure.Data.Tables;
using Data.TableStorage.SchemaUtilities;

namespace Data.TableStorage
{
    public class AdoptionApplication : ITableEntity
    {
        // ITableEntity required properties
        public string PartitionKey { get; set; } // User name
        public string RowKey { get; set; } // User email
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        public string PetName { get; set; }
        public DateTime PetBirthDate { get; set; }
        public DateTime DateSubmitted { get; set; }
        public AdoptionStatus AdoptionStatus { get; set; }
        public string? Notes { get; set; }

        public AdoptionApplication(
            string userName, string userEmail, string petName, DateTime petBirthDate, string? notes)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("User name cannot be null or empty.", nameof(userName));
            }
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                throw new ArgumentException("User email cannot be null or empty.", nameof(userEmail));
            }
            if (string.IsNullOrWhiteSpace(petName))
            {
                throw new ArgumentException("Pet name cannot be null or empty.", nameof(petName));
            }
            if (petBirthDate == default)
            {
                throw new ArgumentException("Pet birth date is not valid.", nameof(petBirthDate));
            }

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
