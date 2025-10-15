using Azure;
using Azure.Data.Tables;
using Data.TableStorage.SchemaUtilities;

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
        public string? Species { get; set; } // Option
        public string? Breed { get; set; } // Option
        public string? Colour { get; set; } // Option
        public Gender Gender { get; set; }
        public Size Size { get; set; }
        public string About { get; set; }
        public AdoptionStatus AdoptionStatus { get; set; }
        public Vaccinations Vaccinations { get; set; }
        public string? MedicalTreatments { get; set; }
        public string? KnownMedicalIssues { get; set; }
        public bool Dewormed { get; set; }
        public bool Chipped { get; set; }
        public string ShelterName { get; set; }
        public string ShelterLocation { get; set; }

        public Pet(string name, DateTime birthDate, string? species, string? breed, string? colour, Gender gender, Size size,
            string about, AdoptionStatus adoptionStatus, Vaccinations vaccinations, string? medicalTreatments,
            string? knownMedicalIssues, bool dewormed, bool chipped, string shelterName, string shelterLocation)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Pet name cannot be null or empty.", nameof(name));
            }
            if (birthDate == default)
            {
                throw new ArgumentException("Pet birth date is not valid.", nameof(birthDate));
            }
            if (birthDate > DateTime.Now)
            {
                throw new ArgumentException("Pet birth date cannot be in the future.", nameof(birthDate));
            }
            if (birthDate < DateTime.Now.AddYears(-25))
            {
                throw new ArgumentException("Pet birth date is not realistic.", nameof(birthDate));
            }


            PartitionKey = name ?? throw new ArgumentNullException(nameof(name));
            RowKey = birthDate.ToString("yyyy-MM-dd");
            Species = species;
            Breed = breed;
            Colour = colour;
            Gender = gender;
            Size = size;
            About = about ?? throw new ArgumentNullException(nameof(about));
            AdoptionStatus = adoptionStatus;
            Vaccinations = vaccinations;
            MedicalTreatments = medicalTreatments;
            KnownMedicalIssues = knownMedicalIssues;
            Dewormed = dewormed;
            Chipped = chipped;
            ShelterName = shelterName ?? throw new ArgumentNullException(nameof(shelterName));
            ShelterLocation = shelterLocation ?? throw new ArgumentNullException(nameof(shelterLocation));
        }
    }
}