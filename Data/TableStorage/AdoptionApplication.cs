using Azure;
using Azure.Data.Tables;

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
        public string AdoptionStatus { get; set; } // Option
        public string Notes { get; set; }

        public AdoptionApplication(string userName, string userEmail)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("User name cannot be null or empty.", nameof(userName));
            }
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                throw new ArgumentException("User email cannot be null or empty.", nameof(userEmail));
            }
            PartitionKey = userName;
            RowKey = userEmail;
        }
    }
}
