namespace PetAdoption.Services.Interfaces;

public interface IGooglePlacesService
{
    /// <summary>
    /// Extracts place details from a Google Maps URL
    /// </summary>
    /// <param name="googleMapsUrl">The Google Maps URL (shortened or full)</param>
    /// <returns>Place details or null if extraction fails</returns>
    Task<GooglePlaceDetails?> GetPlaceDetailsFromUrlAsync(string googleMapsUrl);
    
    /// <summary>
    /// Gets place details using Place ID
    /// </summary>
    /// <param name="placeId">Google Places Place ID</param>
    /// <returns>Place details or null if not found</returns>
    Task<GooglePlaceDetails?> GetPlaceDetailsByIdAsync(string placeId);
}

public class GooglePlaceDetails
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FormattedAddress { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public Location? Location { get; set; }
    public OpeningHours? OpeningHours { get; set; }
    public string? Vicinity { get; set; }
    public List<string> Types { get; set; } = new();
}

public class Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class OpeningHours
{
    public bool OpenNow { get; set; }
    public List<string>? WeekdayText { get; set; }
}