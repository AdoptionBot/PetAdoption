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
        
        // Get unified Google API key from configuration
        _apiKey = configuration["GoogleApiKey"] 
            ?? throw new InvalidOperationException("Google API key not found in configuration. Ensure GoogleApiKey is set.");
    }

    public async Task<GooglePlaceDetails?> GetPlaceDetailsFromUrlAsync(string googleMapsUrl)
    {
        try
        {
            _logger.LogInformation("Starting extraction for URL: {Url}", googleMapsUrl);

            // Step 1: Expand shortened URL if needed
            var expandedUrl = await ExpandShortenedUrlAsync(googleMapsUrl);
            _logger.LogInformation("Expanded URL: {ExpandedUrl}", expandedUrl);
            
            // Step 2: Extract Place ID from URL (try multiple patterns)
            var placeId = ExtractPlaceIdFromUrl(expandedUrl);
            
            if (!string.IsNullOrEmpty(placeId))
            {
                _logger.LogInformation("Extracted Place ID: {PlaceId}", placeId);
                var details = await GetPlaceDetailsByIdAsync(placeId);
                if (details != null)
                {
                    return details;
                }
            }
            
            // Step 3: Try extracting place name and searching
            var placeName = ExtractPlaceNameFromUrl(expandedUrl);
            if (!string.IsNullOrEmpty(placeName))
            {
                _logger.LogInformation("Extracted place name: {PlaceName}", placeName);
                var details = await FindPlaceByNameAsync(placeName);
                if (details != null)
                {
                    return details;
                }
            }
            
            // Step 4: Fallback - Extract coordinates and search nearby
            var (lat, lng) = ExtractCoordinatesFromUrl(expandedUrl);
            if (lat.HasValue && lng.HasValue)
            {
                _logger.LogInformation("Extracted coordinates: {Lat}, {Lng}", lat, lng);
                var details = await FindPlaceByCoordinatesAsync(lat.Value, lng.Value);
                if (details != null)
                {
                    return details;
                }
            }
            
            _logger.LogWarning("Could not extract place information from URL: {Url}", googleMapsUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting place details from URL: {Url}", googleMapsUrl);
            return null;
        }
    }

    public async Task<GooglePlaceDetails?> GetPlaceDetailsByIdAsync(string placeId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            var fields = "place_id,name,formatted_address,formatted_phone_number,international_phone_number," +
                        "website,geometry,vicinity,opening_hours,types";
            
            var url = $"{PlacesApiBaseUrl}/details/json?place_id={placeId}&fields={fields}&key={_apiKey}";
            
            _logger.LogInformation("Fetching place details for Place ID: {PlaceId}", placeId);
            
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonResponse = await response.Content.ReadFromJsonAsync<PlaceDetailsResponse>();
            
            if (jsonResponse?.Status == "OK" && jsonResponse.Result != null)
            {
                _logger.LogInformation("Successfully retrieved place details for: {Name}", jsonResponse.Result.Name);
                return MapToGooglePlaceDetails(jsonResponse.Result);
            }
            
            if (jsonResponse?.Status == "REQUEST_DENIED")
            {
                _logger.LogError("Google Places API request denied. Status: {Status}, Error: {ErrorMessage}. Check your API key and enabled APIs.", 
                    jsonResponse?.Status, jsonResponse?.ErrorMessage);
            }
            else
            {
                _logger.LogWarning("Place not found or API error. Status: {Status}, Error: {ErrorMessage}", 
                    jsonResponse?.Status, jsonResponse?.ErrorMessage);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching place details for Place ID: {PlaceId}", placeId);
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
            _logger.LogInformation("Expanding shortened URL: {Url}", url);
            
            // Create a handler that DOESN'T follow redirects automatically
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            
            var currentUrl = url;
            var redirectCount = 0;
            const int maxRedirects = 10;
            
            // Manually follow redirects to capture each step
            while (redirectCount < maxRedirects)
            {
                var response = await client.GetAsync(currentUrl);
                
                // Check for redirect status codes
                if (response.StatusCode == System.Net.HttpStatusCode.Moved ||
                    response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                    response.StatusCode == System.Net.HttpStatusCode.Found ||
                    response.StatusCode == System.Net.HttpStatusCode.SeeOther ||
                    response.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect ||
                    response.StatusCode == System.Net.HttpStatusCode.PermanentRedirect)
                {
                    if (response.Headers.Location != null)
                    {
                        var nextUrl = response.Headers.Location.IsAbsoluteUri
                            ? response.Headers.Location.ToString()
                            : new Uri(new Uri(currentUrl), response.Headers.Location).ToString();
                        
                        _logger.LogDebug("Redirect {Count}: {From} -> {To}", redirectCount + 1, currentUrl, nextUrl);
                        
                        // Check if we've reached a Google Maps URL
                        if (nextUrl.Contains("google.com/maps"))
                        {
                            // Clean consent URLs if present
                            if (nextUrl.Contains("consent.google.com"))
                            {
                                var match = Regex.Match(nextUrl, @"continue=([^&]+)");
                                if (match.Success)
                                {
                                    nextUrl = Uri.UnescapeDataString(match.Groups[1].Value);
                                }
                            }
                            
                            _logger.LogInformation("Successfully expanded to: {FinalUrl}", nextUrl);
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
                    // No more redirects, check if we're at a Google Maps page
                    if (currentUrl.Contains("google.com/maps"))
                    {
                        _logger.LogInformation("Successfully expanded to: {FinalUrl}", currentUrl);
                        return currentUrl;
                    }
                    
                    // Try to parse HTML for JavaScript redirects
                    var html = await response.Content.ReadAsStringAsync();
                    var jsRedirect = ExtractJavaScriptRedirect(html);
                    if (!string.IsNullOrEmpty(jsRedirect))
                    {
                        _logger.LogInformation("Found JavaScript redirect to: {JsRedirect}", jsRedirect);
                        return jsRedirect;
                    }
                    
                    break;
                }
                else
                {
                    break;
                }
            }
            
            _logger.LogWarning("Could not fully expand URL after {Count} redirects", redirectCount);
            return currentUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expanding shortened URL: {Url}", url);
            return url;
        }
    }

    private string? ExtractJavaScriptRedirect(string html)
    {
        var patterns = new[]
        {
            @"window\.location\.(?:replace|href)\s*=\s*[""']([^""']+)[""']",
            @"<meta[^>]*http-equiv\s*=\s*[""']refresh[""'][^>]*content\s*=\s*[""'][^;]*;\s*url\s*=\s*([^""']+)[""']",
            @"https://www\.google\.com/maps/[^\s""'<>]+"
        };
        
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var extractedUrl = match.Groups[match.Groups.Count > 1 ? 1 : 0].Value;
                extractedUrl = System.Net.WebUtility.HtmlDecode(extractedUrl);
                
                if (extractedUrl.Contains("google.com/maps"))
                {
                    return extractedUrl;
                }
            }
        }
        
        return null;
    }

    private string? ExtractPlaceIdFromUrl(string url)
    {
        // Decode URL to handle encoded characters
        var decodedUrl = Uri.UnescapeDataString(url);
        
        // Pattern 1: !1s followed by Place ID (most common in expanded URLs)
        var match = Regex.Match(decodedUrl, @"!1s([A-Za-z0-9_-]{27,})");
        if (match.Success)
        {
            _logger.LogDebug("Place ID extracted via pattern !1s: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        // Pattern 2: place_id= parameter
        match = Regex.Match(decodedUrl, @"place_id=([A-Za-z0-9_-]+)");
        if (match.Success)
        {
            _logger.LogDebug("Place ID extracted via place_id parameter: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        // Pattern 3: /place/name/PLACE_ID format
        match = Regex.Match(decodedUrl, @"/place/[^/]+/([A-Za-z0-9_-]{20,})(?:[/@]|$)");
        if (match.Success)
        {
            _logger.LogDebug("Place ID extracted via /place/ path: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        // Pattern 4: data= parameter with embedded Place ID
        match = Regex.Match(decodedUrl, @"data=[^!]*!1s([A-Za-z0-9_-]{27,})");
        if (match.Success)
        {
            _logger.LogDebug("Place ID extracted via data parameter: {PlaceId}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
        
        _logger.LogDebug("No Place ID found in URL");
        return null;
    }

    private string? ExtractPlaceNameFromUrl(string url)
    {
        // Try to extract place name from URL
        // Pattern: /place/Place+Name or /place/Place%20Name
        var match = Regex.Match(url, @"/place/([^/@\?#]+)");
        
        if (match.Success)
        {
            var placeName = match.Groups[1].Value;
            // Decode URL encoding (handles %20, %C3%AD etc.)
            placeName = Uri.UnescapeDataString(placeName);
            // Replace + with spaces
            placeName = placeName.Replace("+", " ");
            // Remove trailing slashes or special chars
            placeName = placeName.Trim(' ', '/', '@');
            
            _logger.LogDebug("Extracted place name: {PlaceName}", placeName);
            return placeName;
        }

        return null;
    }

    private (double?, double?) ExtractCoordinatesFromUrl(string url)
    {
        // Decode URL first
        var decodedUrl = Uri.UnescapeDataString(url);
        
        // Patterns to match coordinates
        var patterns = new[]
        {
            @"@(-?\d+\.?\d+),(-?\d+\.?\d+)",           // @lat,lng
            @"ll=(-?\d+\.?\d+),(-?\d+\.?\d+)",         // ll=lat,lng
            @"/(-?\d+\.?\d+),(-?\d+\.?\d+)",           // /lat,lng
            @"!3d(-?\d+\.?\d+)!4d(-?\d+\.?\d+)"        // !3dlat!4dlng (common in data parameter)
        };
        
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(decodedUrl, pattern);
            if (match.Success && match.Groups.Count >= 3)
            {
                if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) && 
                    double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                {
                    _logger.LogDebug("Extracted coordinates: {Lat}, {Lng} using pattern: {Pattern}", lat, lng, pattern);
                    return (lat, lng);
                }
            }
        }
        
        return (null, null);
    }

    private async Task<GooglePlaceDetails?> FindPlaceByNameAsync(string placeName)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            // Use Find Place From Text API to get Place ID
            var url = $"{PlacesApiBaseUrl}/findplacefromtext/json?input={Uri.EscapeDataString(placeName)}" +
                     $"&inputtype=textquery&fields=place_id&key={_apiKey}";
            
            _logger.LogInformation("Searching for place by name: {PlaceName}", placeName);
            
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonResponse = await response.Content.ReadFromJsonAsync<FindPlaceResponse>();
            
            if (jsonResponse?.Status == "OK" && jsonResponse.Candidates?.Any() == true)
            {
                var placeId = jsonResponse.Candidates.First().PlaceId;
                _logger.LogInformation("Found place by name search, Place ID: {PlaceId}", placeId);
                
                // Now get full details using the Place ID
                return await GetPlaceDetailsByIdAsync(placeId);
            }
            
            if (jsonResponse?.Status == "ZERO_RESULTS")
            {
                _logger.LogWarning("No places found for name: {PlaceName}", placeName);
            }
            else if (jsonResponse?.Status != "OK")
            {
                _logger.LogWarning("Find place API returned status: {Status}", jsonResponse?.Status);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding place by name: {PlaceName}", placeName);
            return null;
        }
    }

    private async Task<GooglePlaceDetails?> FindPlaceByCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            // Use Nearby Search API
            var url = $"{PlacesApiBaseUrl}/nearbysearch/json?location={latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}" +
                     $"&radius=50&key={_apiKey}";
            
            _logger.LogInformation("Searching for place by coordinates: {Lat}, {Lng}", latitude, longitude);
            
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonResponse = await response.Content.ReadFromJsonAsync<NearbySearchResponse>();
            
            if (jsonResponse?.Status == "OK" && jsonResponse.Results?.Any() == true)
            {
                // Get the first (closest) result
                var firstResult = jsonResponse.Results.First();
                _logger.LogInformation("Found place by coordinates, Place ID: {PlaceId}, Name: {Name}", 
                    firstResult.PlaceId, firstResult.Name);
                
                // **FIX: Get full details using the Place ID from the response**
                return await GetPlaceDetailsByIdAsync(firstResult.PlaceId);
            }
            
            if (jsonResponse?.Status == "ZERO_RESULTS")
            {
                _logger.LogWarning("No places found near coordinates: {Lat}, {Lng}", latitude, longitude);
            }
            else if (jsonResponse?.Status != "OK")
            {
                _logger.LogWarning("Nearby search API returned status: {Status}", jsonResponse?.Status);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding place by coordinates: {Lat}, {Lng}", latitude, longitude);
            return null;
        }
    }

    private GooglePlaceDetails MapToGooglePlaceDetails(PlaceDetailsResult result)
    {
        return new GooglePlaceDetails
        {
            PlaceId = result.PlaceId ?? string.Empty,
            Name = result.Name ?? string.Empty,
            FormattedAddress = result.FormattedAddress ?? string.Empty,
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
        public string PlaceId { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class FindPlaceResponse
    {
        [JsonPropertyName("candidates")]
        public List<FindPlaceCandidate>? Candidates { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    private class FindPlaceCandidate
    {
        [JsonPropertyName("place_id")]
        public string PlaceId { get; set; } = string.Empty;
    }

    #endregion
}