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
            
            // Step 2: Extract Place ID from the expanded URL (with hex format support)
            var placeId = ExtractPlaceIdFromUrl(expandedUrl);
            
            if (!string.IsNullOrEmpty(placeId))
            {
                _logger.LogInformation("? Extracted Place ID: {PlaceId}", placeId);
                
                // Step 3: Get full details using Place Details API
                var details = await GetPlaceDetailsByIdAsync(placeId);
                if (details != null)
                {
                    _logger.LogInformation("? Successfully retrieved full place details");
                    return details;
                }
                else
                {
                    _logger.LogWarning("? Place Details API returned null for Place ID: {PlaceId}", placeId);
                }
            }
            else
            {
                _logger.LogWarning("? Could not extract Place ID from URL");
            }
            
            // Step 4: Try using coordinates to find the place via Geocoding
            var (lat, lng) = ExtractCoordinatesFromUrl(expandedUrl);
            if (lat.HasValue && lng.HasValue)
            {
                _logger.LogInformation("Attempting to find place using coordinates: {Lat}, {Lng}", lat, lng);
                var details = await FindPlaceByCoordinatesAsync(lat.Value, lng.Value);
                if (details != null)
                {
                    return details;
                }
            }
            
            // Step 5: Fallback - create basic result from URL data
            var placeName = ExtractPlaceNameFromUrl(expandedUrl);
            
            if (!string.IsNullOrEmpty(placeName))
            {
                _logger.LogWarning("Falling back to basic extraction from URL data");
                
                return new GooglePlaceDetails
                {
                    PlaceId = placeId ?? string.Empty,
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
            
            // Request ALL available fields (don't restrict)
            var url = $"{PlacesApiBaseUrl}/details/json?place_id={placeId}&key={_apiKey}";
            
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
                
                // Log what we actually received
                _logger.LogInformation("? Place Details Retrieved:");
                _logger.LogInformation("  - Name: {Name}", result.Name);
                _logger.LogInformation("  - Address: {Address}", result.FormattedAddress);
                _logger.LogInformation("  - Phone (formatted): {Phone}", result.FormattedPhoneNumber ?? "N/A");
                _logger.LogInformation("  - Phone (international): {Phone}", result.InternationalPhoneNumber ?? "N/A");
                _logger.LogInformation("  - Website: {Website}", result.Website ?? "N/A");
                _logger.LogInformation("  - Has Opening Hours: {HasHours}", result.OpeningHours != null);
                
                return MapToGooglePlaceDetails(result);
            }
            
            if (jsonResponse.Status == "REQUEST_DENIED")
            {
                _logger.LogError("? REQUEST_DENIED: {Error}. Check API key restrictions!", 
                    jsonResponse.ErrorMessage);
            }
            else if (jsonResponse.Status == "INVALID_REQUEST")
            {
                _logger.LogError("? INVALID_REQUEST: {Error}. Place ID might be invalid: {PlaceId}", 
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

    private async Task<GooglePlaceDetails?> FindPlaceByCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            // Use Geocoding API to reverse geocode and get a Place ID
            var httpClient = _httpClientFactory.CreateClient();
            
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?" +
                     $"latlng={latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}" +
                     $"&result_type=veterinary_care|point_of_interest|establishment" +
                     $"&key={_apiKey}";
            
            _logger.LogInformation("Using Geocoding API to find place at coordinates");
            
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Geocoding API returned status: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var jsonResponse = await response.Content.ReadFromJsonAsync<GeocodingResponse>();
            
            if (jsonResponse?.Status == "OK" && jsonResponse.Results?.Any() == true)
            {
                // Find the first result that has a Place ID
                foreach (var result in jsonResponse.Results)
                {
                    if (!string.IsNullOrEmpty(result.PlaceId))
                    {
                        _logger.LogInformation("Found Place ID via Geocoding: {PlaceId}", result.PlaceId);
                        
                        // Now get full details using the Place ID
                        var details = await GetPlaceDetailsByIdAsync(result.PlaceId);
                        if (details != null)
                        {
                            return details;
                        }
                    }
                }
            }
            else if (jsonResponse?.Status == "REQUEST_DENIED")
            {
                _logger.LogError("Geocoding API request denied. Ensure Geocoding API is enabled in Google Cloud Console.");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not find place by coordinates (non-critical)");
            return null;
        }
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
        
        // Pattern 1: Hex format (legacy): !1s0x[hex]:0x[hex]
        // Example: !1s0xc605917c356351:0xe3e0dd70813c7eb2
        var match = Regex.Match(decodedUrl, @"!1s(0x[0-9a-fA-F]+:0x[0-9a-fA-F]+)");
        if (match.Success)
        {
            var hexPlaceId = match.Groups[1].Value;
            _logger.LogDebug("? Found hex-format Place ID: {PlaceId}", hexPlaceId);
            
            // Convert hex Place ID to standard format using the hex value directly
            // Google accepts hex format directly in API calls
            return hexPlaceId;
        }
        
        // Pattern 2: Standard format: !1s[PLACE_ID] (27+ alphanumeric chars)
        match = Regex.Match(decodedUrl, @"!1s([A-Za-z0-9_-]{27,})");
        if (match.Success)
        {
            _logger.LogDebug("? Found standard Place ID: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        // Pattern 3: place_id=[PLACE_ID]
        match = Regex.Match(decodedUrl, @"place_id=([A-Za-z0-9_-]+)");
        if (match.Success)
        {
            _logger.LogDebug("? Found Place ID via place_id parameter: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        // Pattern 4: /place/name/[PLACE_ID]
        match = Regex.Match(decodedUrl, @"/place/[^/]+/([A-Za-z0-9_-]{27,})");
        if (match.Success)
        {
            _logger.LogDebug("? Found Place ID in path: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        // Pattern 5: ftid=[PLACE_ID]
        match = Regex.Match(decodedUrl, @"ftid=([A-Za-z0-9_-]{27,})");
        if (match.Success)
        {
            _logger.LogDebug("? Found Place ID via ftid parameter: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        _logger.LogWarning("? Could not extract Place ID using any known pattern");
        _logger.LogDebug("URL was: {Url}", decodedUrl.Substring(0, Math.Min(500, decodedUrl.Length)));
        
        return null;
    }

    private string? ExtractPlaceNameFromUrl(string url)
    {
        var match = Regex.Match(url, @"/place/([^/@\?#]+)");
        
        if (match.Success)
        {
            var placeName = Uri.UnescapeDataString(match.Groups[1].Value);
            placeName = placeName.Replace("+", " ").Trim();
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
            @"@(-?\d+\.?\d+),(-?\d+\.?\d+)",
            @"ll=(-?\d+\.?\d+),(-?\d+\.?\d+)",
            @"!3d(-?\d+\.?\d+)!4d(-?\d+\.?\d+)"
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
            } : null
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