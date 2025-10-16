namespace PetAdoption.Data.TableStorage.Validation
{
    /// <summary>
    /// Helper class for UI validation utilities
    /// </summary>
    public static class ValidationHelpers
    {
        /// <summary>
        /// Gets all valid countries as a sorted list (useful for Blazor dropdowns)
        /// </summary>
        public static List<string> GetCountryList() => IcaoCountryLoader.GetCountryList();
    }
}