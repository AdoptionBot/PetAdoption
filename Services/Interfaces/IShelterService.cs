using PetAdoption.Data.TableStorage;

namespace PetAdoption.Services.Interfaces
{
    public interface IShelterService
    {
        Task<Shelter?> GetShelterAsync(string shelterName, string cityTown);
        Task AddShelterAsync(Shelter shelter);
        Task UpdateShelterAsync(Shelter shelter);
        Task DeleteShelterAsync(string shelterName, string cityTown);
        Task<IEnumerable<Shelter>> GetAllSheltersAsync();
    }
}