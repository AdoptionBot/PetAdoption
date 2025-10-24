using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Data.TableStorage.Validation
{
    /// <summary>
    /// Validates that a URL is well-formed, but allows null or empty values.
    /// </summary>
    public class OptionalUrlAttribute : ValidationAttribute
    {
        private readonly UrlAttribute _urlValidator = new();

        public OptionalUrlAttribute()
        {
            ErrorMessage = "The {0} field must be a valid URL.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Allow null or empty strings
            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                return ValidationResult.Success;
            }

            // If value exists, validate it as a URL
            if (!_urlValidator.IsValid(value))
            {
                var memberName = validationContext.MemberName ?? validationContext.DisplayName;
                return new ValidationResult(
                    string.Format(ErrorMessage ?? "Invalid URL", memberName),
                    new[] { validationContext.MemberName ?? string.Empty }
                );
            }

            return ValidationResult.Success;
        }
    }
}