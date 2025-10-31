using PetAdoption.Data.TableStorage;
using PetAdoption.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace PetAdoption.Services.Data
{
    public class VeterinaryService : IVeterinaryService
    {
        private readonly IAzureTableStorageService _tableStorageService;
        private readonly ILogger<VeterinaryService>? _logger;
        private const string TableName = "Veterinaries";
        private static bool _tableEnsured = false;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public VeterinaryService(IAzureTableStorageService tableStorageService, ILogger<VeterinaryService>? logger = null)
        {
            _tableStorageService = tableStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Ensures the table exists before performing operations
        /// </summary>
        private async Task EnsureTableExistsAsync()
        {
            if (_tableEnsured) return;

            await _semaphore.WaitAsync();
            try
            {
                if (!_tableEnsured)
                {
                    _logger?.LogInformation("Ensuring {TableName} table exists", TableName);
                    await _tableStorageService.CreateTableAsync(TableName);
                    _tableEnsured = true;
                    _logger?.LogInformation("{TableName} table is ready", TableName);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Veterinary?> GetVeterinaryAsync(string veterinaryName, string cityTown)
        {
            await EnsureTableExistsAsync();
            return await _tableStorageService.GetEntityAsync<Veterinary>(TableName, veterinaryName, cityTown);
        }

        public async Task<IEnumerable<Veterinary>> GetAllVeterinariesAsync()
        {
            await EnsureTableExistsAsync();
            return await _tableStorageService.QueryEntitiesAsync<Veterinary>(TableName);
        }

        public async Task AddVeterinaryAsync(Veterinary veterinary)
        {
            await EnsureTableExistsAsync();
            await _tableStorageService.AddEntityAsync(TableName, veterinary);
        }

        public async Task UpdateVeterinaryAsync(Veterinary veterinary)
        {
            await EnsureTableExistsAsync();
            
            // Fetch the latest entity from database to get the current ETag
            var currentVeterinary = await _tableStorageService.GetEntityAsync<Veterinary>(
                TableName, veterinary.PartitionKey, veterinary.RowKey);
            
            if (currentVeterinary == null)
            {
                throw new InvalidOperationException(
                    $"Veterinary not found: {veterinary.PartitionKey} ({veterinary.RowKey})");
            }

            // Update the properties while keeping the fresh ETag
            currentVeterinary.GoogleMapsLocation = veterinary.GoogleMapsLocation;
            currentVeterinary.PhoneNumber = veterinary.PhoneNumber;
            currentVeterinary.Address = veterinary.Address;
            currentVeterinary.Country = veterinary.Country;
            currentVeterinary.Website = veterinary.Website;
            currentVeterinary.OpeningTimes = veterinary.OpeningTimes;

            // Use UpsertEntityAsync which handles ETag internally
            await _tableStorageService.UpsertEntityAsync(TableName, currentVeterinary);
        }

        public async Task DeleteVeterinaryAsync(string veterinaryName, string cityTown)
        {
            await EnsureTableExistsAsync();
            await _tableStorageService.DeleteEntityAsync(TableName, veterinaryName, cityTown);
        }

        public async Task<IEnumerable<Veterinary>> GetVeterinariesByCountryAsync(string country)
        {
            await EnsureTableExistsAsync();
            return await _tableStorageService.QueryEntitiesAsync<Veterinary>(
                TableName,
                filter: $"Country eq '{country}'");
        }
    }
}