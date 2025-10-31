using PetAdoption.Data.TableStorage;

namespace PetAdoption.Services.Interfaces
{
    public interface IVeterinaryService
    {
        Task<Veterinary?> GetVeterinaryAsync(string veterinaryName, string cityTown);
        Task<IEnumerable<Veterinary>> GetAllVeterinariesAsync();
        Task AddVeterinaryAsync(Veterinary veterinary);
        Task UpdateVeterinaryAsync(Veterinary veterinary);
        Task DeleteVeterinaryAsync(string veterinaryName, string cityTown);
        Task<IEnumerable<Veterinary>> GetVeterinariesByCountryAsync(string country);
    }
}