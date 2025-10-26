using Azure;
using Azure.Data.Tables;
using PetAdoption.Data.TableStorage.Enums;
using PetAdoption.Data.TableStorage.Validation;
using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Data.TableStorage
{
    public class Pet : ITableEntity
    {
        // ITableEntity required properties
        [Required(ErrorMessage = "Pet name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Pet name must be between 3 and 50 characters.")]
        public string PartitionKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pet birth date is required.")]
        [ValidBirthDate]
        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom properties
        [Required(ErrorMessage = "Species is required.")]
        [EnumDataType(typeof(Species), ErrorMessage = "Invalid species.")]
        public Species Species { get; set; }

        [StringLength(50, ErrorMessage = "Breed cannot exceed 50 characters.")]
        public string? Breed { get; set; }

        [StringLength(50, ErrorMessage = "Colour cannot exceed 50 characters.")]
        public string? Colour { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender.")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Size is required.")]
        [EnumDataType(typeof(Size), ErrorMessage = "Invalid size.")]
        public Size Size { get; set; }

        [Required(ErrorMessage = "About description is required.")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "About description must be between 1 and 2000 characters.")]
        public string About { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adoption status is required.")]
        [EnumDataType(typeof(AdoptionStatus), ErrorMessage = "Invalid adoption status.")]
        public AdoptionStatus AdoptionStatus { get; set; }

        [Required(ErrorMessage = "Vaccinations status is required.")]
        [EnumDataType(typeof(Vaccinations), ErrorMessage = "Invalid vaccinations status.")]
        public Vaccinations Vaccinations { get; set; }

        [StringLength(1000, ErrorMessage = "Medical treatments cannot exceed 1000 characters.")]
        public string? MedicalTreatments { get; set; }

        [StringLength(1000, ErrorMessage = "Known medical issues cannot exceed 1000 characters.")]
        public string? KnownMedicalIssues { get; set; }

        public bool Neutered { get; set; }
        public bool Dewormed { get; set; }
        public bool Chipped { get; set; }

        [Required(ErrorMessage = "Shelter name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Shelter name must be between 3 and 50 characters.")]
        public string ShelterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shelter location is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Shelter location must be between 3 and 50 characters.")]
        public string ShelterLocation { get; set; } = string.Empty;

        [Url(ErrorMessage = "Image 1 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image1Url { get; set; }

        [Url(ErrorMessage = "Image 2 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image2Url { get; set; }

        [Url(ErrorMessage = "Image 3 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image3Url { get; set; }

        [Url(ErrorMessage = "Image 4 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Image URL cannot exceed 256 characters.")]
        public string? Image4Url { get; set; }

        [Url(ErrorMessage = "Video 1 URL must be a valid URL.")]
        [StringLength(256, ErrorMessage = "Video URL cannot exceed 256 characters.")]
        public string? Video1Url { get; set; }

        // Parameterless constructor for deserialization
        public Pet() { }

        public Pet(string name, DateTime birthDate, Species species, string? breed, string? colour, Gender gender, Size size,
            string about, AdoptionStatus adoptionStatus, Vaccinations vaccinations, string? medicalTreatments,
            string? knownMedicalIssues, bool neutered, bool dewormed, bool chipped, string shelterName, string shelterLocation)
        {
            PartitionKey = name;
            RowKey = birthDate.ToString("yyyy-MM-dd");
            Species = species;
            Breed = breed;
            Colour = colour;
            Gender = gender;
            Size = size;
            About = about;
            AdoptionStatus = adoptionStatus;
            Vaccinations = vaccinations;
            MedicalTreatments = medicalTreatments;
            KnownMedicalIssues = knownMedicalIssues;
            Neutered = neutered;
            Dewormed = dewormed;
            Chipped = chipped;
            ShelterName = shelterName;
            ShelterLocation = shelterLocation;
        }
    }
}