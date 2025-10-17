using PetAdoption.Data.TableStorage;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data
{
    public class PetService : IPetService
    {
        private readonly IAzureTableStorageService _tableStorageService;
        private const string TableName = "Pets";

        public PetService(IAzureTableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        public async Task<Pet?> GetPetAsync(string petName, string birthDate)
        {
            return await _tableStorageService.GetEntityAsync<Pet>(TableName, petName, birthDate);
        }

        public async Task AddPetAsync(Pet pet)
        {
            await _tableStorageService.AddEntityAsync(TableName, pet);
        }

        public async Task UpdatePetAsync(Pet pet)
        {
            await _tableStorageService.UpsertEntityAsync(TableName, pet);
        }

        public async Task DeletePetAsync(string petName, string birthDate)
        {
            await _tableStorageService.DeleteEntityAsync(TableName, petName, birthDate);
        }

        public async Task<IEnumerable<Pet>> GetAllPetsAsync()
        {
            return await _tableStorageService.QueryEntitiesAsync<Pet>(TableName);
        }

        public async Task<IEnumerable<Pet>> GetPetsByShelterAsync(string shelterName, string shelterLocation)
        {
            return await _tableStorageService.QueryEntitiesAsync<Pet>(
                TableName,
                filter: $"ShelterName eq '{shelterName}' and ShelterLocation eq '{shelterLocation}'");
        }
    }
}