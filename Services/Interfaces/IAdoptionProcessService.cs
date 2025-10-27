using PetAdoption.Data.TableStorage;

namespace PetAdoption.Services.Interfaces
{
    /// <summary>
    /// Service for managing adoption process business logic
    /// </summary>
    public interface IAdoptionProcessService
    {
        /// <summary>
        /// Submits an adoption application for a pet
        /// </summary>
        Task<(bool Success, string Message)> SubmitAdoptionApplicationAsync(
            string userName, 
            string userEmail, 
            string petName, 
            string petBirthDate, 
            string? notes = null);

        /// <summary>
        /// Gets all adoption applications for a specific user
        /// </summary>
        Task<IEnumerable<AdoptionApplication>> GetUserAdoptionApplicationsAsync(string userEmail);

        /// <summary>
        /// Gets all adoption applications for pets belonging to a specific shelter
        /// </summary>
        Task<IEnumerable<AdoptionApplication>> GetShelterAdoptionApplicationsAsync(string shelterName, string shelterLocation);

        /// <summary>
        /// Gets the pet associated with an adoption application
        /// </summary>
        Task<Pet?> GetPetForApplicationAsync(AdoptionApplication application);

        /// <summary>
        /// Gets the user who submitted an adoption application
        /// </summary>
        Task<User?> GetUserForApplicationAsync(AdoptionApplication application);

        /// <summary>
        /// Checks if a user already has a pending application for a specific pet
        /// </summary>
        Task<bool> HasPendingApplicationAsync(string userEmail, string petName, string petBirthDate);

        /// <summary>
        /// Shelter accepts an adoption application
        /// </summary>
        Task<(bool Success, string Message)> AcceptApplicationAsync(AdoptionApplication application);

        /// <summary>
        /// Shelter rejects an adoption application
        /// </summary>
        Task<(bool Success, string Message)> RejectApplicationAsync(AdoptionApplication application);

        /// <summary>
        /// User accepts an adoption application that was accepted by shelter
        /// </summary>
        Task<(bool Success, string Message)> UserAcceptApplicationAsync(AdoptionApplication application);

        /// <summary>
        /// User rejects an adoption application that was accepted by shelter
        /// </summary>
        Task<(bool Success, string Message)> UserRejectApplicationAsync(AdoptionApplication application);

        /// <summary>
        /// Shelter confirms the adoption is complete (final step)
        /// </summary>
        Task<(bool Success, string Message)> ConfirmAdoptionCompleteAsync(AdoptionApplication application);
    }
}