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

                var veterinaries = GetMadeiraVeterinaries();

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
        private List<Veterinary> GetMadeiraVeterinaries()
        {
            return new List<Veterinary>
            {
                new Veterinary(
                    "Cl�nica Veterin�ria da Penteada",
                    "Funchal",
                    "https://maps.app.goo.gl/8ZYfXvKjYmPqRLQB8",
                    "+351 291 764 731",
                    "Rua da Penteada, 9000-001 Funchal",
                    "Portugal",
                    "https://www.facebook.com/clinicapenteada",
                    "Mon-Fri: 9:00-19:00, Sat: 9:00-13:00"
                ),
                new Veterinary(
                    "Hospital Veterin�rio do Funchal",
                    "Funchal",
                    "https://maps.app.goo.gl/vTQxH6Qs7rYPBDxu9",
                    "+351 291 229 229",
                    "Rua Ivens, 9000-046 Funchal",
                    "Portugal",
                    "https://www.hvfunchal.com",
                    "Mon-Fri: 9:00-20:00, Sat: 9:00-18:00, Sun: 10:00-14:00"
                ),
                new Veterinary(
                    "Cl�nica Veterin�ria Monte",
                    "Monte",
                    "https://maps.app.goo.gl/9KqRzXvPy8sNJwYt7",
                    "+351 291 782 640",
                    "Caminho do Monte, 9050-288 Funchal",
                    "Portugal",
                    null,
                    "Mon-Fri: 9:00-18:00, Sat: 9:00-12:00"
                ),
                new Veterinary(
                    "Veterin�ria S�o Roque",
                    "Funchal",
                    "https://maps.app.goo.gl/4JhNx2KpRzTyVwMC7",
                    "+351 291 220 360",
                    "Rua de S�o Roque, 9000-214 Funchal",
                    "Portugal",
                    "https://www.facebook.com/VeterinariaSaoRoque",
                    "Mon-Fri: 9:30-19:30, Sat: 9:30-13:00"
                ),
                new Veterinary(
                    "Centro Veterin�rio da Madeira",
                    "C�mara de Lobos",
                    "https://maps.app.goo.gl/3PvQs5NxYtKwHjTy8",
                    "+351 291 943 200",
                    "Estrada Jo�o Gon�alves Zarco, 9300-138 C�mara de Lobos",
                    "Portugal",
                    "https://www.cvm-madeira.com",
                    "Mon-Fri: 9:00-19:00, Sat: 9:00-13:00"
                ),
                new Veterinary(
                    "Cl�nica Veterin�ria de Machico",
                    "Machico",
                    "https://maps.app.goo.gl/7RnYx3PzQwKvTmZY9",
                    "+351 291 965 432",
                    "Rua do Ribeirinho, 9200-108 Machico",
                    "Portugal",
                    null,
                    "Mon-Fri: 9:00-18:00"
                ),
                new Veterinary(
                    "Hospital Veterin�rio de Santa Cruz",
                    "Santa Cruz",
                    "https://maps.app.goo.gl/5QnZw2RvXyPqKwMV7",
                    "+351 291 520 800",
                    "Rua Dr. Jo�o Abel de Freitas, 9100-149 Santa Cruz",
                    "Portugal",
                    "https://www.hvsantacruz.pt",
                    "Mon-Fri: 9:00-20:00, Sat: 9:00-14:00"
                ),
                new Veterinary(
                    "Cl�nica Veterin�ria Ribeira Brava",
                    "Ribeira Brava",
                    "https://maps.app.goo.gl/2MvNx4QyZwLpHmTu6",
                    "+351 291 952 123",
                    "Rua Comandante Camacho de Freitas, 9350-211 Ribeira Brava",
                    "Portugal",
                    null,
                    "Mon-Fri: 9:00-18:00, Sat: 9:00-12:00"
                ),
                new Veterinary(
                    "Veterin�ria Porto Santo",
                    "Porto Santo",
                    "https://maps.app.goo.gl/6YnHx5TzRwQvSmXW8",
                    "+351 291 984 200",
                    "Rua Dr. Nuno Silvestre Teixeira, 9400-164 Porto Santo",
                    "Portugal",
                    null,
                    "Mon-Fri: 9:00-17:00"
                ),
                new Veterinary(
                    "Cl�nica Veterin�ria do Cani�o",
                    "Cani�o",
                    "https://maps.app.goo.gl/8XmYw6VxZyRqLnTZ7",
                    "+351 291 934 567",
                    "Rua do Pedregal, 9125-031 Cani�o",
                    "Portugal",
                    "https://www.facebook.com/VeterinariaCanico",
                    "Mon-Fri: 9:00-19:00, Sat: 9:00-13:00"
                ),
                new Veterinary(
                    "Centro Veterin�rio Ponta do Sol",
                    "Ponta do Sol",
                    "https://maps.app.goo.gl/9PqWx7YzXwSrNmYu9",
                    "+351 291 972 300",
                    "Rua Dr. Jo�o Augusto Teixeira, 9360-219 Ponta do Sol",
                    "Portugal",
                    null,
                    "Mon-Fri: 9:00-18:00"
                ),
                new Veterinary(
                    "Veterin�ria S�o Vicente",
                    "S�o Vicente",
                    "https://maps.app.goo.gl/4LnMx8QzYwPvKwNY8",
                    "+351 291 842 400",
                    "S�tio da Feiteira, 9240-225 S�o Vicente",
                    "Portugal",
                    null,
                    "Mon-Fri: 9:00-17:00"
                )
            };
        }
    }
}