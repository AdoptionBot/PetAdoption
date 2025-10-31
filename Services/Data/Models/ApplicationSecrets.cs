namespace PetAdoption.Services.Data.Models
{
    /// <summary>
    /// Data class to hold authentication secrets retrieved from Azure Key Vault
    /// </summary>
    public class ApplicationSecrets
    {
        public string GoogleMapsApiKey { get; set; } = string.Empty;
        public string GoogleClientId { get; set; } = string.Empty;
        public string GoogleClientSecret { get; set; } = string.Empty;
        public string MicrosoftClientId { get; set; } = string.Empty;
        public string MicrosoftClientSecret { get; set; } = string.Empty;
        //public string AppleClientId { get; set; } = string.Empty;
        //public string AppleTeamId { get; set; } = string.Empty;
        //public string AppleKeyId { get; set; } = string.Empty;
        //public string ApplePrivateKey { get; set; } = string.Empty;
    }
}