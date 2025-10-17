using PetAdoption.Data.TableStorage;

namespace PetAdoption.Services.Interfaces
{
    public interface IPetService
    {
        Task<Pet?> GetPetAsync(string petName, string birthDate);
        Task AddPetAsync(Pet pet);
        Task UpdatePetAsync(Pet pet);
        Task DeletePetAsync(string petName, string birthDate);
        Task<IEnumerable<Pet>> GetAllPetsAsync();
        Task<IEnumerable<Pet>> GetPetsByShelterAsync(string shelterName, string shelterLocation);
    }
}