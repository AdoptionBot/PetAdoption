namespace PetAdoption.Services.Data.Models
{
    /// <summary>
    /// Data class to hold authentication secrets retrieved from Azure Key Vault
    /// </summary>
    public class ApplicationSecrets
    {
        /// <summary>
        /// Unified Google API Key for Maps, Places, Geocoding, and Embed APIs
        /// Replaces separate GoogleMapsApiKey and GooglePlacesApiKey
        /// </summary>
        public string GoogleApiKey { get; set; } = string.Empty;

        public string GoogleClientId { get; set; } = string.Empty;
        public string GoogleClientSecret { get; set; } = string.Empty;
        public string MicrosoftClientId { get; set; } = string.Empty;
        public string MicrosoftClientSecret { get; set; } = string.Empty;
    }
}