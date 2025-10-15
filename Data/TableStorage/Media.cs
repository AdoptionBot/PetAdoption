using Azure;
using Azure.Data.Tables;

namespace Data.TableStorage
{
    public class Media : ITableEntity
    {
        // ITableEntity required properties
        public string PartitionKey { get; set; } // Pet name
        public string RowKey { get; set; } // Pet birth date
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        public string? Image1Url { get; set; }
        public string? Image2Url { get; set; }
        public string? Image3Url { get; set; }
        public string? Image4Url { get; set; }
        public string? Image5Url { get; set; }
        public string? Image6Url { get; set; }
        public string? Image7Url { get; set; }
        public string? Image8Url { get; set; }
        public string? Video1Url { get; set; }

        public Media(string petName, DateTime petBirthDate)
        {
            if (string.IsNullOrWhiteSpace(petName))
            {
                throw new ArgumentException("Pet name cannot be null or empty.", nameof(petName));
            }
            PartitionKey = petName;
            RowKey = petBirthDate.ToString("yyyy-MM-dd");
        }
    }
}