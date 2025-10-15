using Azure;
using Azure.Data.Tables;

namespace Data.TableStorage
{
    public class User : ITableEntity
    {
        // ITableEntity required properties
        public string PartitionKey { get; set; } // User name
        public string RowKey { get; set; } // User email
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Website { get; set; }
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public bool AccountDisabled { get; set; }
        public string Role { get; set; } // Option
        public string ShelterName { get; set; }
        public string ShelterLocation{ get; set; } // Shelter city / town

        public User(string userName, string userEmail)
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