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

        public async Task<AdoptionApplication?> GetApplicationAsync(string partitionKey, string rowKey)
        {
            return await _tableStorageService.GetEntityAsync<AdoptionApplication>(TableName, partitionKey, rowKey);
        }

        public async Task AddApplicationAsync(AdoptionApplication application)
        {
            await _tableStorageService.AddEntityAsync(TableName, application);
        }

        public async Task UpdateApplicationAsync(AdoptionApplication application)
        {
            await _tableStorageService.UpsertEntityAsync(TableName, application);
        }

        public async Task DeleteApplicationAsync(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteEntityAsync(TableName, partitionKey, rowKey);
        }

        public async Task<IEnumerable<AdoptionApplication>> GetAllApplicationsAsync()
        {
            return await _tableStorageService.QueryEntitiesAsync<AdoptionApplication>(TableName);
        }

        public async Task<IEnumerable<AdoptionApplication>> GetApplicationsByUserEmailAsync(string userEmail)
        {
            return await _tableStorageService.QueryEntitiesAsync<AdoptionApplication>(
                TableName,
                filter: $"UserEmail eq '{userEmail}'");
        }

        public async Task<IEnumerable<AdoptionApplication>> GetApplicationsByStatusAsync(AdoptionStatus status)
        {
            return await _tableStorageService.QueryEntitiesAsync<AdoptionApplication>(
                TableName,
                filter: $"AdoptionStatus eq '{status}'");
        }

        public async Task<IEnumerable<AdoptionApplication>> GetApplicationsByPetAsync(string petName, string petBirthDate)
        {
            return await _tableStorageService.QueryEntitiesAsync<AdoptionApplication>(
                TableName,
                filter: $"PetName eq '{petName}' and PetBirthDateString eq '{petBirthDate}'");
        }

        public async Task<IEnumerable<AdoptionApplication>> GetApplicationsByUserAndPetAsync(string userEmail, string petName, string petBirthDate)
        {
            return await _tableStorageService.QueryEntitiesAsync<AdoptionApplication>(
                TableName,
                filter: $"UserEmail eq '{userEmail}' and PetName eq '{petName}' and PetBirthDateString eq '{petBirthDate}'");
        }
    }
}