using PetAdoption.Data.TableStorage;
using PetAdoption.Data.TableStorage.Enums;

namespace PetAdoption.Services.Interfaces
{
    public interface IAdoptionApplicationService
    {
        Task<AdoptionApplication?> GetApplicationAsync(string userEmail, string petBirthDate);
        Task AddApplicationAsync(AdoptionApplication application);
        Task UpdateApplicationAsync(AdoptionApplication application);
        Task DeleteApplicationAsync(string userEmail, string petBirthDate);
        Task<IEnumerable<AdoptionApplication>> GetAllApplicationsAsync();
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByUserAsync(string userEmail);
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByStatusAsync(AdoptionStatus status);
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByPetAsync(string petName);
        Task<IEnumerable<AdoptionApplication>> GetApplicationsByUserAndPetAsync(string userEmail, string petName, string petBirthDate);
    }
}