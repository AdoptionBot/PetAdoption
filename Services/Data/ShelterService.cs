using PetAdoption.Data.TableStorage;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data
{
    public class ShelterService : IShelterService
    {
        private readonly IAzureTableStorageService _tableStorageService;
        private const string TableName = "Shelters";

        public ShelterService(IAzureTableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        public async Task<Shelter?> GetShelterAsync(string shelterName, string cityTown)
        {
            return await _tableStorageService.GetEntityAsync<Shelter>(TableName, shelterName, cityTown);
        }

        public async Task AddShelterAsync(Shelter shelter)
        {
            await _tableStorageService.AddEntityAsync(TableName, shelter);
        }

        public async Task UpdateShelterAsync(Shelter shelter)
        {
            await _tableStorageService.UpsertEntityAsync(TableName, shelter);
        }

        public async Task DeleteShelterAsync(string shelterName, string cityTown)
        {
            await _tableStorageService.DeleteEntityAsync(TableName, shelterName, cityTown);
        }

        public async Task<IEnumerable<Shelter>> GetAllSheltersAsync()
        {
            return await _tableStorageService.QueryEntitiesAsync<Shelter>(TableName);
        }
    }
}