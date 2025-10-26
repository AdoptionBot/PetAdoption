using PetAdoption.Data.TableStorage;
using PetAdoption.Data.TableStorage.Enums;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data
{
    public class AdoptionApplicationService : IAdoptionApplicationService
    {
        private readonly IAzureTableStorageService _tableStorageService;
        private const string TableName = "AdoptionApplications";

        public AdoptionApplicationService(IAzureTableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        public async Task<AdoptionApplication?> GetApplicationAsync(string userName, string userEmail)
        {
            return await _tableStorageService.GetEntityAsync<AdoptionApplication>(TableName, userName, userEmail);
        }

        public async Task AddApplicationAsync(AdoptionApplication application)
        {
            await _tableStorageService.AddEntityAsync(TableName, application);
        }

        public async Task UpdateApplicationAsync(AdoptionApplication application)
        {
            await _tableStorageService.UpsertEntityAsync(TableName, application);
        }

        public async Task DeleteApplicationAsync(string userName, string userEmail)
        {
            await _tableStorageService.DeleteEntityAsync(TableName, userName, userEmail);
        }

        public async Task<IEnumerable<AdoptionApplication>> GetAllApplicationsAsync()
        {
            return await _tableStorageService.QueryEntitiesAsync<AdoptionApplication>(TableName);
        }

        public async Task<IEnumerable<AdoptionApplication>> GetApplicationsByUserAsync(string userName)
        {
            return await _tableStorageService.QueryEntitiesAsync<AdoptionApplication>(
                TableName,
                filter: $"PartitionKey eq '{userName}'");
        }

        public async Task<IEnumerable<AdoptionApplication>> GetApplicationsByStatusAsync(AdoptionStatus status)
        {
            return await _tableStorageService.QueryEntitiesAsync<AdoptionApplication>(
                TableName,
                filter: $"AdoptionStatus eq '{status}'");
        }

        public async Task<IEnumerable<AdoptionApplication>> GetApplicationsByPetAsync(string petName)
        {
            return await _tableStorageService.QueryEntitiesAsync<AdoptionApplication>(
                TableName,
                filter: $"PetName eq '{petName}'");
        }
    }
}