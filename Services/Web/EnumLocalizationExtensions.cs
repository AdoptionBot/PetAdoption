using Microsoft.Extensions.Localization;
using PetAdoption.Data.TableStorage.Enums;
using System.Globalization;

namespace PetAdoption.Services.Web
{
    /// <summary>
    /// Extension methods for localizing enum values
    /// Storage maintains English values while display uses current culture
    /// </summary>
    public static class EnumLocalizationExtensions
    {
        public static string ToLocalizedString(this AdoptionStatus status, IStringLocalizer<SharedEnumResources> localizer)
        {
            var key = $"AdoptionStatus_{status}";
            return localizer[key];
        }

        public static string ToLocalizedString(this Gender gender, IStringLocalizer<SharedEnumResources> localizer)
        {
            var key = $"Gender_{gender}";
            return localizer[key];
        }

        public static string ToLocalizedString(this Size size, IStringLocalizer<SharedEnumResources> localizer)
        {
            var key = $"Size_{size}";
            return localizer[key];
        }

        public static string ToLocalizedString(this Species species, IStringLocalizer<SharedEnumResources> localizer)
        {
            var key = $"Species_{species}";
            return localizer[key];
        }

        public static string ToLocalizedString(this UserRole role, IStringLocalizer<SharedEnumResources> localizer)
        {
            var key = $"UserRole_{role}";
            return localizer[key];
        }

        public static string ToLocalizedString(this Vaccinations vaccination, IStringLocalizer<SharedEnumResources> localizer)
        {
            var key = $"Vaccinations_{vaccination}";
            return localizer[key];
        }

        /// <summary>
        /// Gets all localized values for a specific enum type
        /// Useful for dropdowns and selects
        /// </summary>
        public static IEnumerable<(T Value, string Display)> GetLocalizedValues<T>(IStringLocalizer<SharedEnumResources> localizer) 
            where T : struct, Enum
        {
            var enumType = typeof(T);
            var prefix = enumType.Name;

            foreach (T value in Enum.GetValues<T>())
            {
                var key = $"{prefix}_{value}";
                yield return (value, localizer[key]);
            }
        }

        /// <summary>
        /// Gets localized strings for flags enum (e.g., Vaccinations)
        /// </summary>
        public static IEnumerable<string> ToLocalizedStrings(this Vaccinations vaccinations, IStringLocalizer<SharedEnumResources> localizer)
        {
            if (vaccinations == Vaccinations.none)
            {
                yield return localizer["Vaccinations_none"];
                yield break;
            }

            foreach (Vaccinations value in Enum.GetValues<Vaccinations>())
            {
                if (value != Vaccinations.none && vaccinations.HasFlag(value))
                {
                    yield return localizer[$"Vaccinations_{value}"];
                }
            }
        }
    }
}