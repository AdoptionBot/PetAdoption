using Microsoft.Extensions.Logging;
using PetAdoption.Data.TableStorage;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Service for seeding initial data into Azure Table Storage
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly IAzureTableStorageService _tableStorageService;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(IAzureTableStorageService tableStorageService, ILogger<DatabaseSeeder> logger)
        {
            _tableStorageService = tableStorageService;
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

                var veterinaries = GetDefaultVeterinaries();

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
        /// Returns a list of veterinary clinics in Madeira
        /// </summary>
        private static List<Veterinary> GetDefaultVeterinaries()
        {
            return new List<Veterinary>
            {
                new Veterinary(
                    "Clínica Veterinária da Madeira",
                    "Funchal",
                    "https://www.google.com/maps/place/32.6489,-16.9117/@32.6489,-16.9117,15z",
                    "+351 291 764 620",
                    "Rua da Levada de São João, 9000-191 Funchal",
                    "Portugal",
                    "https://www.clinicaveterinariamadeira.com",
                    "Mon-Fri: 9:00-19:00, Sat: 9:00-13:00",
                    null  // No photo reference for seeded entries
                ),
                new Veterinary(
                    "Hospital Veterinário do Funchal",
                    "Funchal",
                    "https://www.google.com/maps/place/32.6500,-16.9200/@32.6500,-16.9200,15z",
                    "+351 291 220 570",
                    "Estrada Monumental, 9000-098 Funchal",
                    "Portugal",
                    null,
                    "24/7 Emergency Service",
                    null  // No photo reference for seeded entries
                ),
                new Veterinary(
                    "Veterinária Ponta do Sol",
                    "Ponta do Sol",
                    "https://www.google.com/maps/place/32.6800,-17.1000/@32.6800,-17.1000,15z",
                    "+351 291 972 300",
                    "Rua Dr. João Augusto Teixeira, 9360-219 Ponta do Sol",
                    "Portugal",
                    null,
                    "Mon-Fri: 9:00-18:00",
                    null  // No photo reference for seeded entries
                ),
                new Veterinary(
                    "Veterinária São Vicente",
                    "São Vicente",
                    "https://www.google.com/maps/place/32.7950,-17.0450/@32.7950,-17.0450,15z",
                    "+351 291 842 400",
                    "Sítio da Feiteira, 9240-225 São Vicente",
                    "Portugal",
                    null,
                    "Mon-Fri: 9:00-17:00",
                    null  // No photo reference for seeded entries
                )
            };
        }
    }
}