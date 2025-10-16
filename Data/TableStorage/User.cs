using Azure;
using Azure.Data.Tables;
using Data.TableStorage.SchemaUtilities;

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
        public string Country { get; set; } // ICAO xml
        public string? Website { get; set; }
        public string? Facebook { get; set; }
        public string? Instagram { get; set; }
        public bool AccountDisabled { get; set; }
        public UserRole Role { get; set; }
        public string? ShelterName { get; set; }
        public string? ShelterLocation{ get; set; }

        public User(string userName, string userEmail, string phoneNumber, string address, string country, string? website,
            string? facebook, string? instagram, bool accountDisabled, UserRole role, string? shelterName, string? shelterLocation)
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
            PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Country = country ?? throw new ArgumentNullException(nameof(country));
            Website = website;
            Facebook = facebook;
            Instagram = instagram;
            AccountDisabled = accountDisabled;
            Role = role;
            ShelterName = shelterName;
            ShelterLocation = shelterLocation;
        }
    }
}