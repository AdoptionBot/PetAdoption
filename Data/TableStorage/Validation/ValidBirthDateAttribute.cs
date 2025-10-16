using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Data.TableStorage.Validation
{
    public class ValidBirthDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Birth date cannot be empty.");
            }

            string dateString = value.ToString()!;

            // Try to parse the date string (expected format: yyyy-MM-dd)
            if (!DateTime.TryParseExact(dateString, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime birthDate))
            {
                return new ValidationResult("Birth date must be in yyyy-MM-dd format.");
            }

            // Check if birth date is in the future
            if (birthDate > DateTime.Today)
            {
                return new ValidationResult("Birth date cannot be in the future.");
            }

            // Check if birth date is more than 25 years ago
            DateTime twentyFiveYearsAgo = DateTime.Today.AddYears(-25);
            if (birthDate < twentyFiveYearsAgo)
            {
                return new ValidationResult("Birth date cannot be more than 25 years ago.");
            }

            return ValidationResult.Success;
        }
    }
}
