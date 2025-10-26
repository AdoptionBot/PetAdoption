using PetAdoption.Data.TableStorage;
using PetAdoption.Data.TableStorage.Enums;

namespace PetAdoption.Services.Interfaces
{
    public interface IAdoptionApplicationService
    {
        Task<AdoptionApplication?> GetApplicationAsync(string userName, string userEmail);
        Task AddApplicationAsync(AdoptionApplication application);
        Task UpdateApplicationAsync(AdoptionApplication application);
        Task DeleteApplicationAsync(string userName, string userEmail);
        Task<IEnumerable<AdoptionApplication>> GetAllApplicationsAsync();
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByUserAsync(string userName);
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByStatusAsync(AdoptionStatus status);
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByPetAsync(string petName);
    }
}