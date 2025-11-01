using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Globalization;
using PetAdoption.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PetAdoption.Services.Data;

public class GooglePlacesService : IGooglePlacesService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly ILogger<GooglePlacesService> _logger;
    private const string PlacesApiBaseUrl = "https://maps.googleapis.com/maps/api/place";

    public GooglePlacesService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GooglePlacesService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        
        _apiKey = configuration["GoogleApiKey"] 
            ?? throw new InvalidOperationException("Google API key not found in configuration. Ensure GoogleApiKey is set.");
    }

    public async Task<GooglePlaceDetails?> GetPlaceDetailsFromUrlAsync(string googleMapsUrl)
    {
        try
        {
            _logger.LogInformation("Starting extraction for URL: {Url}", googleMapsUrl);

            // Step 1: Expand shortened URL
            var expandedUrl = await ExpandShortenedUrlAsync(googleMapsUrl);
            _logger.LogInformation("Successfully expanded to: {ExpandedUrl}", expandedUrl);
            
            // Step 2: Try to extract standard Place ID first (most reliable)
            var placeId = ExtractPlaceIdFromUrl(expandedUrl);
            
            if (!string.IsNullOrEmpty(placeId) && !placeId.StartsWith("0x"))
            {
                _logger.LogInformation("? Extracted standard Place ID: {PlaceId}", placeId);
                
                var details = await GetPlaceDetailsByIdAsync(placeId);
                if (details != null && IsValidVeterinaryPlace(details))
                {
                    _logger.LogInformation("? Successfully retrieved full place details via Place ID");
                    return details;
                }
                else if (details != null)
                {
                    _logger.LogWarning("Place ID returned data but doesn't appear to be a veterinary business");
                }
                else
                {
                    _logger.LogWarning("? Place Details API returned null for Place ID: {PlaceId}", placeId);
                }
            }
            else if (!string.IsNullOrEmpty(placeId) && placeId.StartsWith("0x"))
            {
                _logger.LogInformation("Found hex-format Place ID: {PlaceId}, will use coordinates instead", placeId);
            }
            else
            {
                _logger.LogWarning("? Could not extract standard Place ID from URL");
            }
            
            // Step 3: Extract place name and coordinates for Text Search
            var placeName = ExtractPlaceNameFromUrl(expandedUrl);
            var (lat, lng) = ExtractCoordinatesFromUrl(expandedUrl);
            
            if (!string.IsNullOrEmpty(placeName) && lat.HasValue && lng.HasValue)
            {
                _logger.LogInformation("? Extracted place name: {PlaceName} and coordinates: {Lat}, {Lng}", 
                    placeName, lat, lng);
                    
                // Try Text Search first (most accurate for businesses)
                var details = await TextSearchAsync(placeName, lat.Value, lng.Value);
                if (details != null && IsValidVeterinaryPlace(details))
                {
                    _logger.LogInformation("? Successfully found place via Text Search");
                    return details;
                }
            }
            
            // Step 4: Try Nearby Search with larger radius
            if (lat.HasValue && lng.HasValue)
            {
                _logger.LogInformation("Attempting Nearby Search at coordinates: {Lat}, {Lng}", lat, lng);
                
                var details = await FindPlaceByCoordinatesAsync(lat.Value, lng.Value);
                if (details != null && IsValidVeterinaryPlace(details))
                {
                    _logger.LogInformation("? Successfully retrieved place details via Nearby Search");
                    return details;
                }
                else
                {
                    _logger.LogWarning("? Nearby Search didn't find a valid veterinary business");
                }
            }
            else
            {
                _logger.LogWarning("? Could not extract coordinates from URL");
            }
            
            // Step 5: Last resort - create basic result from URL data
            if (!string.IsNullOrEmpty(placeName))
            {
                _logger.LogWarning("All API methods failed. Falling back to basic extraction from URL data");
                
                return new GooglePlaceDetails
                {
                    PlaceId = string.Empty,
                    Name = placeName,
                    FormattedAddress = "Address extraction failed - please add manually",
                    PhoneNumber = null,
                    Website = null,
                    Location = lat.HasValue && lng.HasValue ? new Location
                    {
                        Latitude = lat.Value,
                        Longitude = lng.Value
                    } : null,
                    OpeningHours = null,
                    Types = new List<string> { "veterinary_care" }
                };
            }
            
            _logger.LogError("Failed to extract any usable information from URL: {Url}", googleMapsUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetPlaceDetailsFromUrlAsync for URL: {Url}", googleMapsUrl);
            return null;
        }
    }

    public async Task<GooglePlaceDetails?> GetPlaceDetailsByIdAsync(string placeId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            // Request comprehensive fields from Places API
            var fields = "place_id,name,formatted_address,formatted_phone_number,international_phone_number,website,geometry,opening_hours,types,vicinity,business_status,photos";
            var url = $"{PlacesApiBaseUrl}/details/json?place_id={Uri.EscapeDataString(placeId)}&fields={fields}&key={_apiKey}";
            
            _logger.LogInformation("Calling Place Details API for Place ID: {PlaceId}", placeId);
            
            var response = await httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("API Response Status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("HTTP {StatusCode}: {Body}", response.StatusCode, 
                    responseBody.Length > 200 ? responseBody.Substring(0, 200) : responseBody);
                return null;
            }
            
            var jsonResponse = await response.Content.ReadFromJsonAsync<PlaceDetailsResponse>();
            
            if (jsonResponse == null)
            {
                _logger.LogError("Failed to deserialize API response");
                return null;
            }
            
            _logger.LogInformation("API Status: {Status}", jsonResponse.Status);
            
            if (jsonResponse.Status == "OK" && jsonResponse.Result != null)
            {
                var result = jsonResponse.Result;
                
                _logger.LogInformation("? Place Details Retrieved:");
                _logger.LogInformation("  - Name: {Name}", result.Name);
                _logger.LogInformation("  - Address: {Address}", result.FormattedAddress);
                _logger.LogInformation("  - Phone (formatted): {Phone}", result.FormattedPhoneNumber ?? "N/A");
                _logger.LogInformation("  - Phone (international): {Phone}", result.InternationalPhoneNumber ?? "N/A");
                _logger.LogInformation("  - Website: {Website}", result.Website ?? "N/A");
                _logger.LogInformation("  - Has Opening Hours: {HasHours}", result.OpeningHours != null);
                _logger.LogInformation("  - Business Status: {Status}", result.BusinessStatus ?? "N/A");
                _logger.LogInformation("  - Types: {Types}", result.Types != null ? string.Join(", ", result.Types) : "N/A");
                
                return MapToGooglePlaceDetails(result);
            }
            
            if (jsonResponse.Status == "REQUEST_DENIED")
            {
                _logger.LogError("? REQUEST_DENIED: {Error}. Check API key and enabled APIs in Google Cloud Console!", 
                    jsonResponse.ErrorMessage);
            }
            else if (jsonResponse.Status == "INVALID_REQUEST")
            {
                _logger.LogError("? INVALID_REQUEST: {Error}. Place ID format invalid: {PlaceId}", 
                    jsonResponse.ErrorMessage, placeId);
            }
            else if (jsonResponse.Status == "NOT_FOUND")
            {
                _logger.LogWarning("? NOT_FOUND: Place ID {PlaceId} not found in Google's database", placeId);
            }
            else
            {
                _logger.LogWarning("API returned status: {Status} with message: {Message}", 
                    jsonResponse.Status, jsonResponse.ErrorMessage);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetPlaceDetailsByIdAsync for Place ID: {PlaceId}", placeId);
            return null;
        }
    }

    private async Task<GooglePlaceDetails?> TextSearchAsync(string placeName, double latitude, double longitude)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            // Use Text Search API with location bias for most accurate results
            var location = $"{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}";
            var radius = "100"; // 100 meters
            
            var url = $"{PlacesApiBaseUrl}/textsearch/json?" +
                     $"query={Uri.EscapeDataString(placeName)}" +
                     $"&location={location}" +
                     $"&radius={radius}" +
                     $"&type=veterinary_care" +
                     $"&key={_apiKey}";
            
            _logger.LogInformation("Using Text Search API for: {PlaceName}", placeName);
            
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Text Search API returned status: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var jsonResponse = await response.Content.ReadFromJsonAsync<TextSearchResponse>();
            
            if (jsonResponse?.Status == "OK" && jsonResponse.Results?.Any() == true)
            {
                // Get the first result (closest match)
                var firstResult = jsonResponse.Results.First();
                
                if (!string.IsNullOrEmpty(firstResult.PlaceId))
                {
                    _logger.LogInformation("? Found Place ID via Text Search: {PlaceId} ({Name})", 
                        firstResult.PlaceId, firstResult.Name);
                    
                    // Get full details
                    return await GetPlaceDetailsByIdAsync(firstResult.PlaceId);
                }
            }
            else if (jsonResponse?.Status == "REQUEST_DENIED")
            {
                _logger.LogError("Text Search API request denied. Ensure Places API is enabled.");
            }
            else if (jsonResponse?.Status == "ZERO_RESULTS")
            {
                _logger.LogInformation("Text Search found no results for: {PlaceName}", placeName);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not find place by text search (non-critical)");
            return null;
        }
    }

    private async Task<GooglePlaceDetails?> FindPlaceByCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            // Use Nearby Search with 150m radius and rank by distance
            var location = $"{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}";
            var radius = "150"; // Increased to 150 meters
            
            var url = $"{PlacesApiBaseUrl}/nearbysearch/json?" +
                     $"location={location}" +
                     $"&radius={radius}" +
                     $"&type=veterinary_care" +
                     $"&rankby=prominence" +
                     $"&key={_apiKey}";
            
            _logger.LogInformation("Using Nearby Search API at coordinates: {Lat}, {Lng} with {Radius}m radius", 
                latitude, longitude, radius);
            
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nearby Search API returned status: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var jsonResponse = await response.Content.ReadFromJsonAsync<NearbySearchResponse>();
            
            if (jsonResponse?.Status == "OK" && jsonResponse.Results?.Any() == true)
            {
                // Log all results found
                _logger.LogInformation("Found {Count} nearby veterinary places", jsonResponse.Results.Count);
                
                foreach (var result in jsonResponse.Results.Take(3))
                {
                    _logger.LogDebug("  - {Name} at {Vicinity}", result.Name, result.Vicinity);
                    
                    if (!string.IsNullOrEmpty(result.PlaceId))
                    {
                        _logger.LogInformation("? Trying Place ID: {PlaceId} ({Name})", result.PlaceId, result.Name);
                        
                        // Get full details
                        var details = await GetPlaceDetailsByIdAsync(result.PlaceId);
                        if (details != null && IsValidVeterinaryPlace(details))
                        {
                            _logger.LogInformation("? Found valid veterinary place: {Name}", details.Name);
                            return details;
                        }
                    }
                }
                
                _logger.LogWarning("No valid veterinary businesses found in nearby search results");
            }
            else if (jsonResponse?.Status == "REQUEST_DENIED")
            {
                _logger.LogError("Nearby Search API request denied. Ensure Places API is enabled.");
            }
            else if (jsonResponse?.Status == "ZERO_RESULTS")
            {
                _logger.LogInformation("No veterinary places found within {Radius}m radius", radius);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not find place by nearby search");
            return null;
        }
    }

    private bool IsValidVeterinaryPlace(GooglePlaceDetails place)
    {
        // Check if the place is actually a veterinary business
        // and not just a street address or generic location
        
        // Must have a meaningful name (not just coordinates or addresses)
        if (string.IsNullOrWhiteSpace(place.Name) || 
            place.Name.StartsWith("CAM da") || 
            place.Name.StartsWith("Rua ") ||
            place.Name.All(char.IsDigit))
        {
            _logger.LogDebug("Invalid place name: {Name}", place.Name);
            return false;
        }
        
        // Check if types include veterinary-related terms
        var veterinaryTypes = new[] { "veterinary_care", "veterinarian", "veterinary", "pet_store" };
        if (place.Types?.Any(t => veterinaryTypes.Contains(t.ToLowerInvariant())) == true)
        {
            _logger.LogDebug("? Valid veterinary type found in: {Types}", string.Join(", ", place.Types));
            return true;
        }
        
        // Check if name contains veterinary-related keywords
        var lowerName = place.Name.ToLowerInvariant();
        var veterinaryKeywords = new[] { "vet", "veterinár", "clínica", "clinic", "animal", "pet" };
        
        if (veterinaryKeywords.Any(keyword => lowerName.Contains(keyword)))
        {
            _logger.LogDebug("? Valid veterinary keyword found in name: {Name}", place.Name);
            return true;
        }
        
        _logger.LogDebug("Place doesn't appear to be a veterinary business: {Name}, Types: {Types}", 
            place.Name, place.Types != null ? string.Join(", ", place.Types) : "none");
        
        return false;
    }

    private async Task<string> ExpandShortenedUrlAsync(string url)
    {
        if (!url.Contains("goo.gl") && !url.Contains("maps.app.goo.gl"))
        {
            return url;
        }

        try
        {
            _logger.LogInformation("Expanding shortened URL...");
            
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            var currentUrl = url;
            var redirectCount = 0;
            const int maxRedirects = 10;
            
            while (redirectCount < maxRedirects)
            {
                var response = await client.GetAsync(currentUrl);
                
                if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                {
                    if (response.Headers.Location != null)
                    {
                        var nextUrl = response.Headers.Location.IsAbsoluteUri
                            ? response.Headers.Location.ToString()
                            : new Uri(new Uri(currentUrl), response.Headers.Location).ToString();
                        
                        if (nextUrl.Contains("google.com/maps"))
                        {
                            if (nextUrl.Contains("consent.google.com"))
                            {
                                var match = Regex.Match(nextUrl, @"continue=([^&]+)");
                                if (match.Success)
                                {
                                    nextUrl = Uri.UnescapeDataString(match.Groups[1].Value);
                                }
                            }
                            
                            return nextUrl;
                        }
                        
                        currentUrl = nextUrl;
                        redirectCount++;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (response.IsSuccessStatusCode)
                {
                    if (currentUrl.Contains("google.com/maps"))
                    {
                        return currentUrl;
                    }
                    break;
                }
                else
                {
                    break;
                }
            }
            
            return currentUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expanding URL");
            return url;
        }
    }

    private string? ExtractPlaceIdFromUrl(string url)
    {
        var decodedUrl = Uri.UnescapeDataString(url);
        
        _logger.LogDebug("Attempting to extract Place ID from URL...");
        
        // Pattern 1: Standard Place ID format: !1s[PLACE_ID] (27+ alphanumeric chars)
        // This is the VALID format for API calls
        var match = Regex.Match(decodedUrl, @"!1s([A-Za-z0-9_-]{27,})");
        if (match.Success)
        {
            var placeId = match.Groups[1].Value;
            // Make sure it's not a hex format
            if (!placeId.StartsWith("0x"))
            {
                _logger.LogDebug("? Found standard Place ID: {PlaceId}", placeId);
                return placeId;
            }
        }
        
        // Pattern 2: place_id=[PLACE_ID]
        match = Regex.Match(decodedUrl, @"place_id=([A-Za-z0-9_-]{27,})");
        if (match.Success)
        {
            _logger.LogDebug("? Found Place ID via place_id parameter: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        // Pattern 3: /place/name/[PLACE_ID]
        match = Regex.Match(decodedUrl, @"/place/[^/]+/([A-Za-z0-9_-]{27,})");
        if (match.Success)
        {
            _logger.LogDebug("? Found Place ID in path: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        // Pattern 4: ftid=[PLACE_ID]
        match = Regex.Match(decodedUrl, @"ftid=([A-Za-z0-9_-]{27,})");
        if (match.Success)
        {
            _logger.LogDebug("? Found Place ID via ftid parameter: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        // Pattern 5: Hex format (legacy): !1s0x[hex]:0x[hex]
        // NOTE: This is NOT valid for API calls, only used for coordinate extraction
        match = Regex.Match(decodedUrl, @"!1s(0x[0-9a-fA-F]+:0x[0-9a-fA-F]+)");
        if (match.Success)
        {
            var hexPlaceId = match.Groups[1].Value;
            _logger.LogDebug("? Found hex-format Place ID (legacy): {PlaceId} - will use coordinates instead", hexPlaceId);
            return hexPlaceId; // Return it but calling code should check for 0x prefix
        }
        
        _logger.LogWarning("? Could not extract Place ID using any known pattern");
        
        return null;
    }

    private string? ExtractPlaceNameFromUrl(string url)
    {
        var match = Regex.Match(url, @"/place/([^/@\?#]+)");
        
        if (match.Success)
        {
            var placeName = Uri.UnescapeDataString(match.Groups[1].Value);
            placeName = placeName.Replace("+", " ").Replace("%20", " ").Trim();
            _logger.LogDebug("Extracted place name: {PlaceName}", placeName);
            return placeName;
        }

        return null;
    }

    private (double?, double?) ExtractCoordinatesFromUrl(string url)
    {
        var decodedUrl = Uri.UnescapeDataString(url);
        
        var patterns = new[]
        {
            @"@(-?\d+\.?\d+),(-?\d+\.?\d+)",           // @lat,lng
            @"ll=(-?\d+\.?\d+),(-?\d+\.?\d+)",          // ll=lat,lng
            @"!3d(-?\d+\.?\d+)!4d(-?\d+\.?\d+)",        // !3dlat!4dlng
            @"center=(-?\d+\.?\d+)%2C(-?\d+\.?\d+)"     // center=lat%2Clng
        };
        
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(decodedUrl, pattern);
            if (match.Success && match.Groups.Count >= 3)
            {
                if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) && 
                    double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                {
                    _logger.LogDebug("Extracted coordinates: {Lat}, {Lng}", lat, lng);
                    return (lat, lng);
                }
            }
        }
        
        return (null, null);
    }

    private GooglePlaceDetails MapToGooglePlaceDetails(PlaceDetailsResult result)
    {
        // Log photo information
        if (result.Photos != null && result.Photos.Any())
        {
            _logger.LogInformation("? Found {Count} photos. First photo reference: {PhotoRef}", 
                result.Photos.Count, result.Photos.First().PhotoReference);
        }
        else
        {
            _logger.LogWarning("? No photos found for this place");
        }

        return new GooglePlaceDetails
        {
            PlaceId = result.PlaceId ?? string.Empty,
            Name = result.Name ?? string.Empty,
            FormattedAddress = result.FormattedAddress ?? result.Vicinity ?? "Address not available",
            PhoneNumber = result.InternationalPhoneNumber ?? result.FormattedPhoneNumber,
            Website = result.Website,
            Vicinity = result.Vicinity,
            Types = result.Types ?? new List<string>(),
            Location = result.Geometry != null ? new Location
            {
                Latitude = result.Geometry.Location.Lat,
                Longitude = result.Geometry.Location.Lng
            } : null,
            OpeningHours = result.OpeningHours != null ? new OpeningHours
            {
                OpenNow = result.OpeningHours.OpenNow,
                WeekdayText = result.OpeningHours.WeekdayText
            } : null,
            PhotoReference = result.Photos?.FirstOrDefault()?.PhotoReference
        };
    }

    #region API Response Models

    private class PlaceDetailsResponse
    {
        [JsonPropertyName("result")]
        public PlaceDetailsResult? Result { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    private class PlaceDetailsResult
    {
        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }
        
        [JsonPropertyName("formatted_phone_number")]
        public string? FormattedPhoneNumber { get; set; }
        
        [JsonPropertyName("international_phone_number")]
        public string? InternationalPhoneNumber { get; set; }
        
        [JsonPropertyName("website")]
        public string? Website { get; set; }
        
        [JsonPropertyName("vicinity")]
        public string? Vicinity { get; set; }
        
        [JsonPropertyName("geometry")]
        public GeometryResult? Geometry { get; set; }
        
        [JsonPropertyName("opening_hours")]
        public OpeningHoursResult? OpeningHours { get; set; }
        
        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }
        
        [JsonPropertyName("business_status")]
        public string? BusinessStatus { get; set; }
        
        [JsonPropertyName("photos")]
        public List<PhotoResult>? Photos { get; set; }
    }

    private class PhotoResult
    {
        [JsonPropertyName("photo_reference")]
        public string? PhotoReference { get; set; }
        
        [JsonPropertyName("height")]
        public int Height { get; set; }
        
        [JsonPropertyName("width")]
        public int Width { get; set; }
    }

    private class GeometryResult
    {
        [JsonPropertyName("location")]
        public LocationResult Location { get; set; } = new();
    }

    private class LocationResult
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        
        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    private class OpeningHoursResult
    {
        [JsonPropertyName("open_now")]
        public bool OpenNow { get; set; }
        
        [JsonPropertyName("weekday_text")]
        public List<string>? WeekdayText { get; set; }
    }

    private class TextSearchResponse
    {
        [JsonPropertyName("results")]
        public List<TextSearchResult>? Results { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    private class TextSearchResult
    {
        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }
    }

    private class NearbySearchResponse
    {
        [JsonPropertyName("results")]
        public List<NearbySearchResult>? Results { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    private class NearbySearchResult
    {
        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("vicinity")]
        public string? Vicinity { get; set; }
        
        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }
    }

    private class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public List<GeocodingResult>? Results { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    private class GeocodingResult
    {
        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }
        
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }
        
        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }
    }

    #endregion
}