using Microsoft.Extensions.Logging;
using PetAdoption.Data.TableStorage;
using PetAdoption.Data.TableStorage.Enums;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.AdoptionProcess
{
    public class AdoptionProcessService : IAdoptionProcessService
    {
        private readonly IAdoptionApplicationService _applicationService;
        private readonly IPetService _petService;
        private readonly IUserService _userService;
        private readonly ILogger<AdoptionProcessService> _logger;

        public AdoptionProcessService(
            IAdoptionApplicationService applicationService,
            IPetService petService,
            IUserService userService,
            ILogger<AdoptionProcessService> logger)
        {
            _applicationService = applicationService;
            _petService = petService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> SubmitAdoptionApplicationAsync(
            string userName, 
            string userEmail, 
            string petName, 
            string petBirthDate, 
            string? notes = null)
        {
            try
            {
                // Validate user exists
                var user = await _userService.GetUserByEmailAsync(userEmail);
                if (user == null)
                {
                    return (false, "User not found. Please ensure you have a complete profile.");
                }

                // Validate pet exists
                var pet = await _petService.GetPetAsync(petName, petBirthDate);
                if (pet == null)
                {
                    return (false, "Pet not found.");
                }

                // Check if pet is available for adoption
                if (pet.AdoptionStatus != AdoptionStatus.NotAdopted)
                {
                    return (false, $"This pet is no longer available for adoption. Current status: {pet.AdoptionStatus}");
                }

                // Check for existing application
                if (await HasPendingApplicationAsync(userEmail, petName, petBirthDate))
                {
                    return (false, "You already have a pending application for this pet.");
                }

                // Parse birth date and ensure it's UTC
                if (!DateTime.TryParse(petBirthDate, out var parsedBirthDate))
                {
                    return (false, "Invalid pet birth date format.");
                }
                
                // Ensure the DateTime is UTC
                var utcBirthDate = DateTime.SpecifyKind(parsedBirthDate, DateTimeKind.Utc);

                // Create adoption application
                var application = new AdoptionApplication(
                    userName,
                    userEmail,
                    petName,
                    utcBirthDate,
                    notes);

                await _applicationService.AddApplicationAsync(application);

                // Update pet status to Submitted
                pet.AdoptionStatus = AdoptionStatus.Submitted;
                await _petService.UpdatePetAsync(pet);

                _logger.LogInformation(
                    "Adoption application submitted: User {UserName} ({UserEmail}) for Pet {PetName}",
                    userName, userEmail, petName);

                return (true, $"Your adoption application for {petName} has been submitted successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error submitting adoption application for user {UserEmail} and pet {PetName}", 
                    userEmail, petName);
                return (false, $"An error occurred while submitting your application: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> AcceptApplicationAsync(AdoptionApplication application)
        {
            try
            {
                // Get the pet
                var pet = await GetPetForApplicationAsync(application);
                if (pet == null)
                {
                    return (false, "Pet not found.");
                }

                // Verify the pet is in Submitted status
                if (pet.AdoptionStatus != AdoptionStatus.Submitted)
                {
                    return (false, $"Cannot accept application. Pet status is: {pet.AdoptionStatus}");
                }

                // Update pet status to AcceptedByShelter
                pet.AdoptionStatus = AdoptionStatus.AcceptedByShelter;
                await _petService.UpdatePetAsync(pet);

                // Update application status
                application.AdoptionStatus = AdoptionStatus.AcceptedByShelter;
                await _applicationService.UpdateApplicationAsync(application);

                _logger.LogInformation(
                    "Adoption application accepted by shelter: User {UserEmail} for Pet {PetName}",
                    application.Email, application.PetName);

                return (true, $"Application for {application.PetName} has been accepted!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error accepting adoption application for pet {PetName}", 
                    application.PetName);
                return (false, $"An error occurred while accepting the application: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RejectApplicationAsync(AdoptionApplication application)
        {
            try
            {
                // Get the pet
                var pet = await GetPetForApplicationAsync(application);
                if (pet == null)
                {
                    return (false, "Pet not found.");
                }

                // Verify the pet is in Submitted status
                if (pet.AdoptionStatus != AdoptionStatus.Submitted)
                {
                    return (false, $"Cannot reject application. Pet status is: {pet.AdoptionStatus}");
                }

                // Update pet status back to NotAdopted
                pet.AdoptionStatus = AdoptionStatus.NotAdopted;
                await _petService.UpdatePetAsync(pet);

                // Delete the application
                await _applicationService.DeleteApplicationAsync(
                    application.PartitionKey, 
                    application.RowKey);

                _logger.LogInformation(
                    "Adoption application rejected by shelter: User {UserEmail} for Pet {PetName}",
                    application.Email, application.PetName);

                return (true, $"Application for {application.PetName} has been rejected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error rejecting adoption application for pet {PetName}", 
                    application.PetName);
                return (false, $"An error occurred while rejecting the application: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ConfirmAdoptionCompleteAsync(AdoptionApplication application)
        {
            try
            {
                // Get the pet
                var pet = await GetPetForApplicationAsync(application);
                if (pet == null)
                {
                    return (false, "Pet not found.");
                }

                // Verify the pet is in AcceptedByShelter status
                if (pet.AdoptionStatus != AdoptionStatus.AcceptedByShelter)
                {
                    return (false, $"Cannot complete adoption. Pet status is: {pet.AdoptionStatus}. The shelter must accept the application first.");
                }

                // Update pet status to Adopted (final state)
                pet.AdoptionStatus = AdoptionStatus.Adopted;
                await _petService.UpdatePetAsync(pet);

                // Update application status to Adopted
                application.AdoptionStatus = AdoptionStatus.Adopted;
                await _applicationService.UpdateApplicationAsync(application);

                _logger.LogInformation(
                    "Adoption completed: User {UserEmail} adopted Pet {PetName}",
                    application.Email, application.PetName);

                return (true, $"Adoption of {application.PetName} has been successfully completed! Congratulations to the new family!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error completing adoption for pet {PetName}", 
                    application.PetName);
                return (false, $"An error occurred while completing the adoption: {ex.Message}");
            }
        }

        public async Task<IEnumerable<AdoptionApplication>> GetUserAdoptionApplicationsAsync(string userEmail)
        {
            try
            {
                // Get all applications for this user (by email as PartitionKey)
                return (await _applicationService.GetApplicationsByUserAsync(userEmail))
                    .OrderByDescending(a => a.DateSubmitted)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications for user {UserEmail}", userEmail);
                return Enumerable.Empty<AdoptionApplication>();
            }
        }

        public async Task<IEnumerable<AdoptionApplication>> GetShelterAdoptionApplicationsAsync(
            string shelterName, 
            string shelterLocation)
        {
            try
            {
                // Get all pets belonging to this shelter
                var shelterPets = await _petService.GetPetsByShelterAsync(shelterName, shelterLocation);
                
                // Create a set of pet identifiers (name + birthdate)
                var petIdentifiers = shelterPets
                    .Select(p => new { Name = p.PartitionKey, BirthDate = p.RowKey })
                    .ToHashSet();

                // Get all applications
                var allApplications = await _applicationService.GetAllApplicationsAsync();

                // Filter applications for pets belonging to this shelter
                return allApplications
                    .Where(a => petIdentifiers.Any(p => 
                        p.Name == a.PetName && 
                        p.BirthDate == a.RowKey))
                    .OrderByDescending(a => a.DateSubmitted)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error getting applications for shelter {ShelterName}", 
                    shelterName);
                return Enumerable.Empty<AdoptionApplication>();
            }
        }

        public async Task<Pet?> GetPetForApplicationAsync(AdoptionApplication application)
        {
            try
            {
                return await _petService.GetPetAsync(
                    application.PetName, 
                    application.RowKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error getting pet for application: {PetName}", 
                    application.PetName);
                return null;
            }
        }

        public async Task<User?> GetUserForApplicationAsync(AdoptionApplication application)
        {
            try
            {
                return await _userService.GetUserByEmailAsync(application.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error getting user for application: {UserEmail}", 
                    application.Email);
                return null;
            }
        }

        public async Task<bool> HasPendingApplicationAsync(
            string userEmail, 
            string petName, 
            string petBirthDate)
        {
            try
            {
                var userApplications = await GetUserAdoptionApplicationsAsync(userEmail);
                
                return userApplications.Any(a => 
                    a.PetName == petName && 
                    a.RowKey == petBirthDate &&
                    (a.AdoptionStatus == AdoptionStatus.Submitted || 
                     a.AdoptionStatus == AdoptionStatus.AcceptedByShelter));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error checking pending applications for user {UserEmail} and pet {PetName}", 
                    userEmail, petName);
                return false;
            }
        }
    }
}