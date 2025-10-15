using Azure;
using Azure.Data.Tables;

namespace Data.TableStorage
{
    public class Pet : ITableEntity
    {
        // ITableEntity required properties
        public string PartitionKey { get; set; } // Pet Name
        public string RowKey { get; set; } // Pet Birth Date
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        public string Species { get; set; } // Option
        public string Breed { get; set; } // Option
        public string Colour { get; set; } // Option
        public string Gender { get; set; } // Option
        public string Size { get; set; } // Option
        public string About { get; set; }
        public string Personality { get; set; } // Option
        public string AdoptionStatus { get; set; } // Option
        public string Vaccinations { get; set; } // Option+Option
        public string MedicalTreatments { get; set; }
        public string KnownMedicalIssues { get; set; }
        public bool Dewormed { get; set; }
        public bool Chipped { get; set; }
        public string ShelterName { get; set; }
        public string ShelterLocation { get; set; } // Shelter city / town

        public Pet(string name, DateTime birthDate)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Pet name cannot be null or empty.", nameof(name));
            }
            PartitionKey = name;
            RowKey = birthDate.ToString("yyyy-MM-dd");
        }
    }
}