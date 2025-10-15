using Azure;
using Azure.Data.Tables;

namespace Data.TableStorage
{
    public class Shelter : ITableEntity
    {
        // ITableEntity required properties
        public string PartitionKey { get; set; } // Shelter name
        public string RowKey { get; set; } // Shelter city / town
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        public string Country { get; set; } // ICAO xml
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string? Website { get; set; }
        public string? Facebook { get; set; }
        public string? Instagram { get; set; }

        public Shelter(string shelterName, string shelterCityTown, string country, string phoneNumber,
            string address, string email, string? website, string? facebook, string? instagram)
        {
            if (string.IsNullOrWhiteSpace(shelterName))
            {
                throw new ArgumentException("Shelter name cannot be null or empty.", nameof(shelterName));
            }
            if (string.IsNullOrWhiteSpace(shelterCityTown))
            {
                throw new ArgumentException("Shelter city / town cannot be null or empty.", nameof(shelterCityTown));
            }

            PartitionKey = shelterName;
            RowKey = shelterCityTown;
            Country = country ?? throw new ArgumentNullException(nameof(country));
            PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Website = website;
            Facebook = facebook;
            Instagram = instagram;
        }
    }
}