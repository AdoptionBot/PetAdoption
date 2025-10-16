using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Data.TableStorage.Validation
{
    /// <summary>
    /// Validates that a country value is a valid ICAO country designation from the XML file
    /// </summary>
    public class ValidIcaoCountryAttribute : ValidationAttribute
    {
        public ValidIcaoCountryAttribute()
        {
            ErrorMessage = "The country '{0}' is not a valid ICAO country designation.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Country is required.");
            }

            var country = value.ToString()!;

            if (!IcaoCountryLoader.IsValidCountry(country))
            {
                var validCountries = IcaoCountryLoader.GetCountryList();
                return new ValidationResult(
                    $"The country '{country}' is not a valid ICAO country designation. " +
                    $"Please select from the valid country list."
                );
            }

            return ValidationResult.Success;
        }
    }
}