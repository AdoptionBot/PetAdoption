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
                if (await HasPendingApplicationAsync(userName, userEmail, petName, petBirthDate))
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
                    "Error submitting adoption application for user {UserName} and pet {PetName}", 
                    userName, petName);
                return (false, $"An error occurred while submitting your application: {ex.Message}");
            }
        }

        public async Task<IEnumerable<AdoptionApplication>> GetUserAdoptionApplicationsAsync(string userName, string userEmail)
        {
            try
            {
                var allApplications = await _applicationService.GetAllApplicationsAsync();
                
                // Filter by both userName (PartitionKey) and userEmail (RowKey)
                return allApplications
                    .Where(a => a.PartitionKey == userName && a.RowKey == userEmail)
                    .OrderByDescending(a => a.DateSubmitted)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications for user {UserName}", userName);
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
                var petNames = shelterPets.Select(p => p.PartitionKey).Distinct().ToHashSet();

                // Get all applications
                var allApplications = await _applicationService.GetAllApplicationsAsync();

                // Filter applications for pets belonging to this shelter
                return allApplications
                    .Where(a => petNames.Contains(a.PetName))
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
                    application.PetBirthDate.ToString("yyyy-MM-dd"));
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
                return await _userService.GetUserByEmailAsync(application.RowKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error getting user for application: {UserEmail}", 
                    application.RowKey);
                return null;
            }
        }

        public async Task<bool> HasPendingApplicationAsync(
            string userName, 
            string userEmail, 
            string petName, 
            string petBirthDate)
        {
            try
            {
                var userApplications = await GetUserAdoptionApplicationsAsync(userName, userEmail);
                
                return userApplications.Any(a => 
                    a.PetName == petName && 
                    a.PetBirthDate.ToString("yyyy-MM-dd") == petBirthDate &&
                    (a.AdoptionStatus == AdoptionStatus.Submitted || 
                     a.AdoptionStatus == AdoptionStatus.AcceptedByShelter ||
                     a.AdoptionStatus == AdoptionStatus.AcceptedByUser));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error checking pending applications for user {UserName} and pet {PetName}", 
                    userName, petName);
                return false;
            }
        }
    }
}