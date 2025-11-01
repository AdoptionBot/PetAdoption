using Microsoft.Extensions.Logging;
using PetAdoption.Data.TableStorage;
using PetAdoption.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Service for seeding initial data into Azure Table Storage
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly IAzureTableStorageService _tableStorageService;
        private readonly IGooglePlacesService _googlePlacesService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(
            IAzureTableStorageService tableStorageService, 
            IGooglePlacesService googlePlacesService,
            IConfiguration configuration,
            ILogger<DatabaseSeeder> logger)
        {
            _tableStorageService = tableStorageService;
            _googlePlacesService = googlePlacesService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Seeds all required tables with initial data
        /// </summary>
        public async Task SeedDatabaseAsync()
        {
            _logger.LogInformation("Starting database seeding...");

            try
            {
                await EnsureTablesExistAsync();
                await SeedVeterinariesAsync();
                
                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed database");
                throw;
            }
        }

        /// <summary>
        /// Ensures all required tables exist
        /// </summary>
        private async Task EnsureTablesExistAsync()
        {
            _logger.LogInformation("Ensuring all tables exist...");

            var tables = new[] { "Users", "Shelters", "Pets", "AdoptionApplications", "Veterinaries" };

            foreach (var tableName in tables)
            {
                try
                {
                    await _tableStorageService.CreateTableAsync(tableName);
                    _logger.LogInformation("Table '{TableName}' is ready", tableName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create table '{TableName}'", tableName);
                    throw;
                }
            }
        }

        /// <summary>
        /// Seeds veterinary data for Madeira
        /// </summary>
        private async Task SeedVeterinariesAsync()
        {
            _logger.LogInformation("Seeding veterinary data...");

            try
            {
                // Check if we already have veterinaries
                var existingVets = await _tableStorageService.QueryEntitiesAsync<Veterinary>("Veterinaries");
                if (existingVets.Any())
                {
                    _logger.LogInformation("Veterinaries table already contains {Count} records. Skipping seed.", existingVets.Count());
                    return;
                }

                var veterinaries = await GetDefaultVeterinariesAsync();

                foreach (var vet in veterinaries)
                {
                    try
                    {
                        await _tableStorageService.AddEntityAsync("Veterinaries", vet);
                        _logger.LogInformation("Added veterinary: {Name} in {Location}", vet.PartitionKey, vet.RowKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to add veterinary: {Name}", vet.PartitionKey);
                    }
                }

                _logger.LogInformation("Veterinary seeding completed. Added {Count} records.", veterinaries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed veterinaries");
                throw;
            }
        }

        /// <summary>
        /// Returns a list of veterinary clinics in Madeira by extracting data from Google Maps URLs
        /// </summary>
        private async Task<List<Veterinary>> GetDefaultVeterinariesAsync()
        {
            var defaultCountry = _configuration["DefaultCountryValue"] ?? "Portugal";
            
            // List of Google Maps URLs for veterinary clinics
            var googleMapsUrls = new List<string>
            {
                "https://maps.app.goo.gl/tSWXMZ4XwnB9yVuc6",
                "https://maps.app.goo.gl/ZjF7iGpiyvHs3huo9",
                "https://maps.app.goo.gl/WFid22FFJepvZDre7",
                "https://maps.app.goo.gl/mvw4DeKxM643mQx16",
                "https://maps.app.goo.gl/eaHjh6dypEp4nS3z8",
                "https://maps.app.goo.gl/nmw5MLANGsdaunK46",
                "https://maps.app.goo.gl/89LRw7WN2LUP2YHb9",
                "https://maps.app.goo.gl/yaJkJC2NHb2aehPo7",
                "https://maps.app.goo.gl/8cLsdRY2CurNJThX9",
                "https://maps.app.goo.gl/TGZkZVDinQhvMm4L6",
                "https://maps.app.goo.gl/KsJG1sDabC199uqz9",
                "https://maps.app.goo.gl/tG3kvDASQNgJDcos8",
                "https://maps.app.goo.gl/7DTU2uajHd2LnJYS7",
                "https://maps.app.goo.gl/KaTR2PuudoZXGU726",
                "https://maps.app.goo.gl/jtHkZTC5unSZtj3e9",
                "https://maps.app.goo.gl/viUNS3XczAuEb3fL9",
                "https://maps.app.goo.gl/E4ry7QS14dhQfA9A6",
                "https://maps.app.goo.gl/jTaoEzfTRJWLZmAA8",
                "https://maps.app.goo.gl/u4qt43cvyyXWcoUT7",
                "https://maps.app.goo.gl/c7JZyV7xxH5nNwcD7",
                "https://maps.app.goo.gl/GDpPeUHn6RXLJhyS8",
                "https://maps.app.goo.gl/GXA5mkiR1jQy6uNE9",
                "https://maps.app.goo.gl/oxpn2qyuHxWQCoH3A",
                "https://maps.app.goo.gl/65EURefk7jg2Ud5G7",
                "https://maps.app.goo.gl/goh8MNWGfwyhk5f68",
                "https://maps.app.goo.gl/E1Tg2h2eQ1FcySpe9",
                "https://maps.app.goo.gl/BoXqffrXMnF1XKyT8",
                "https://maps.app.goo.gl/dSgGZzmSGLKuAErCA",
                "https://maps.app.goo.gl/ZAjCwTkPFkSe56Bt6",
                "https://maps.app.goo.gl/58Pjc1RG9PXYMKk58",
                "https://maps.app.goo.gl/oWG5PVWHjKK4hLBF6",
                "https://maps.app.goo.gl/PHWnWm9P3eaviSsf6",
                "https://maps.app.goo.gl/GRDwp1XWF5xWnmnBA",
                "https://maps.app.goo.gl/p52CBo2ZoX2MUHaA9",
                "https://maps.app.goo.gl/jptg7sXHpiGPnQDbA",
                "https://maps.app.goo.gl/axhkzYqb7p5jFm277",
                "https://maps.app.goo.gl/yzuXGaxqPUroyAmS7"
            };

            var veterinaries = new List<Veterinary>();

            foreach (var url in googleMapsUrls)
            {
                try
                {
                    _logger.LogInformation("Extracting veterinary information from URL: {Url}", url);
                    
                    // Extract place details from Google Maps URL
                    var placeDetails = await _googlePlacesService.GetPlaceDetailsFromUrlAsync(url);
                    
                    if (placeDetails == null)
                    {
                        _logger.LogWarning("Could not extract place details from URL: {Url}", url);
                        continue;
                    }

                    // Extract city from address
                    var city = ExtractCityFromAddress(placeDetails.FormattedAddress) ?? "Madeira";

                    // Format opening hours
                    var openingTimes = FormatOpeningHours(placeDetails.OpeningHours?.WeekdayText);

                    // Create veterinary entity
                    var veterinary = new Veterinary(
                        veterinaryName: CleanVeterinaryName(placeDetails.Name),
                        cityTown: city,
                        googleMapsLocation: url,
                        phoneNumber: placeDetails.PhoneNumber,
                        address: placeDetails.FormattedAddress,
                        country: defaultCountry,
                        website: placeDetails.Website,
                        openingTimes: openingTimes,
                        photoReference: placeDetails.PhotoReference
                    );

                    veterinaries.Add(veterinary);
                    
                    _logger.LogInformation("? Successfully extracted: {Name} in {City}", veterinary.PartitionKey, veterinary.RowKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract veterinary from URL: {Url}", url);
                }
            }

            // If no veterinaries were extracted, log warning
            if (!veterinaries.Any())
            {
                _logger.LogWarning("No veterinaries were extracted from Google Maps URLs. Database will be empty.");
            }

            return veterinaries;
        }

        /// <summary>
        /// Cleans and validates the veterinary name
        /// </summary>
        private string CleanVeterinaryName(string name)
        {
            name = name.Trim();

            // Ensure it's within the character limit
            if (name.Length > 100)
            {
                name = name.Substring(0, 100);
                _logger.LogWarning("Veterinary name truncated to 100 characters");
            }

            return name;
        }

        /// <summary>
        /// Extracts city/town from a formatted address
        /// </summary>
        private string? ExtractCityFromAddress(string? address)
        {
            if (string.IsNullOrEmpty(address)) return null;

            // Common Madeira cities
            var cityPatterns = new[]
            {
                "Funchal", "Câmara de Lobos", "Ribeira Brava", "Machico",
                "Santa Cruz", "Calheta", "Ponta do Sol", "São Vicente",
                "Santana", "Porto Moniz", "Ponta Delgada", "Porto Santo"
            };

            // Check if any known city is in the address
            foreach (var city in cityPatterns)
            {
                if (address.Contains(city, StringComparison.OrdinalIgnoreCase))
                {
                    return city;
                }
            }

            // Fallback: try to extract from address format "Street, City, Postal Code, Country"
            var parts = address.Split(',');
            if (parts.Length >= 2)
            {
                var potentialCity = parts[1].Trim();
                
                // Remove postal codes if present
                potentialCity = System.Text.RegularExpressions.Regex.Replace(
                    potentialCity, 
                    @"\d{4}-\d{3}", 
                    ""
                ).Trim();
                
                if (potentialCity.Length >= 2 && potentialCity.Length <= 50)
                {
                    return potentialCity;
                }
            }

            return "Madeira"; // Default fallback
        }

        /// <summary>
        /// Formats opening hours from weekday text array
        /// </summary>
        private string? FormatOpeningHours(List<string>? weekdayText)
        {
            if (weekdayText == null || !weekdayText.Any())
            {
                return null;
            }

            // Join with line breaks for better readability
            // Limit to 500 characters to comply with validation
            var formatted = string.Join(", ", weekdayText.Take(7));
            
            if (formatted.Length > 500)
            {
                formatted = formatted.Substring(0, 497) + "...";
            }

            return formatted;
        }
    }
}