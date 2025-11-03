using PetAdoption.Data.TableStorage;
using PetAdoption.Data.TableStorage.Enums;

namespace PetAdoption.Services.Interfaces
{
    public interface IAdoptionApplicationService
    {
        Task<AdoptionApplication?> GetApplicationAsync(string partitionKey, string rowKey);
        Task AddApplicationAsync(AdoptionApplication application);
        Task UpdateApplicationAsync(AdoptionApplication application);
        Task DeleteApplicationAsync(string partitionKey, string rowKey);
        Task<IEnumerable<AdoptionApplication>> GetAllApplicationsAsync();
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByUserEmailAsync(string userEmail);
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByStatusAsync(AdoptionStatus status);
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByPetAsync(string petName, string petBirthDate);
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByUserAndPetAsync(string userEmail, string petName, string petBirthDate);
    }
}