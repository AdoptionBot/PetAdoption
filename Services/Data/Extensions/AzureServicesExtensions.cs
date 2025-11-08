using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetAdoption.Services.AdoptionProcess;
using PetAdoption.Services.Data;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data.Extensions
{
    /// <summary>
    /// Extensions for registering Azure services with dependency injection
    /// Follows best practices for service lifetimes and separation of concerns
    /// </summary>
    public static class AzureServicesExtensions
    {
        /// <summary>
        /// Adds Azure Table Storage service as Singleton
        /// TableServiceClient is thread-safe and should be reused
        /// </summary>
        public static IServiceCollection AddAzureTableStorageService(
            this IServiceCollection services,
            string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
            }

            services.AddSingleton<IAzureTableStorageService>(sp =>
            {
                var logger = sp.GetService<ILogger<AzureTableStorageService>>();
                return new AzureTableStorageService(connectionString, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds Azure Blob Storage client factory as Singleton and service as Scoped
        /// BlobServiceClient is thread-safe; service is scoped for request isolation
        /// </summary>
        public static IServiceCollection AddAzureBlobStorageService(
            this IServiceCollection services,
            string connectionString,
            string containerName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
            }

            // Register factory as Singleton (creates one BlobServiceClient for the app)
            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<AzureBlobStorageClientFactory>>();
                return new AzureBlobStorageClientFactory(connectionString, logger);
            });

            // Register service as Scoped (per-request instance using shared client)
            services.AddScoped<IAzureBlobStorageService>(sp =>
            {
                var factory = sp.GetRequiredService<AzureBlobStorageClientFactory>();
                var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
                return new AzureBlobStorageService(factory, containerName, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds named blob storage service for a specific container
        /// </summary>
        private static void AddNamedBlobStorageService(
            IServiceCollection services,
            string containerName,
            string serviceName)
        {
            services.AddKeyedScoped<IAzureBlobStorageService>(serviceName, (sp, key) =>
            {
                var factory = sp.GetRequiredService<AzureBlobStorageClientFactory>();
                var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
                return new AzureBlobStorageService(factory, containerName, logger);
            });
        }

        /// <summary>
        /// Adds all business logic services as Scoped
        /// Business services should be scoped to ensure proper data isolation per request
        /// </summary>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IShelterService, ShelterService>();
            services.AddScoped<IPetService, PetService>();
            services.AddScoped<IAdoptionApplicationService, AdoptionApplicationService>();
            services.AddScoped<IAdoptionProcessService, AdoptionProcessService>();
            services.AddScoped<IVeterinaryService, VeterinaryService>();
            services.AddScoped<IGooglePlacesService, GooglePlacesService>();
            
            // Register GooglePlacesPhotoService as Scoped
            services.AddScoped<GooglePlacesPhotoService>();

            return services;
        }

        /// <summary>
        /// Adds all Azure services with Key Vault integration
        /// Orchestrates service registration in correct order with proper lifetimes
        /// </summary>
        public static async Task<IServiceCollection> AddAllAzureServicesAsync(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Validate configuration
            var keyVaultUri = configuration["KeyVault:VaultUri"];
            var secretName = configuration["KeyVault:AzureStorageConnectionStringSecret"];
            var petMediaContainer = configuration["Azure:BlobStorage:PetMediaContainer"];
            var shelterDocumentsContainer = configuration["Azure:BlobStorage:ShelterDocumentsContainer"];
            var veterinaryMediaContainer = configuration["Azure:BlobStorage:VeterinaryMediaContainer"];
            var shelterMediaContainer = configuration["Azure:BlobStorage:ShelterMediaContainer"];

            if (string.IsNullOrWhiteSpace(keyVaultUri))
            {
                throw new InvalidOperationException(
                    "KeyVault:VaultUri is not configured in appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new InvalidOperationException(
                    "KeyVault:AzureStorageConnectionStringSecret is not configured in appsettings.json");
            }

            // Set defaults for container names
            petMediaContainer = string.IsNullOrWhiteSpace(petMediaContainer) ? "pet-media" : petMediaContainer;
            shelterDocumentsContainer = string.IsNullOrWhiteSpace(shelterDocumentsContainer) 
                ? "shelter-documents" 
                : shelterDocumentsContainer;
            veterinaryMediaContainer = string.IsNullOrWhiteSpace(veterinaryMediaContainer)
                ? "veterinary-media"
                : veterinaryMediaContainer;
            shelterMediaContainer = string.IsNullOrWhiteSpace(shelterMediaContainer)
                ? "shelter-media"
                : shelterMediaContainer;

            // Step 1: Retrieve secrets from Key Vault (happens once at startup)
            var tempKeyVaultService = KeyVaultSecretService.CreateInstance(configuration);
            var storageConnectionString = await tempKeyVaultService.GetStorageConnectionStringAsync();

            // Retrieve Font Awesome Kit URL from Key Vault
            var fontAwesomeKitUrl = await tempKeyVaultService.GetSecretAsync(
                "FontAwesomeKitUrlSecret", 
                "https://kit.fontawesome.com/f03b6c9128.js"); // Fallback to default if secret not found
            
            // Store Font Awesome Kit URL in configuration for access throughout the app
            configuration["FontAwesomeKitUrl"] = fontAwesomeKitUrl;

            // Step 2: Register KeyVaultSecretService as Singleton
            services.AddSingleton<KeyVaultSecretService>();

            // Step 3: Register Azure Table Storage as Singleton
            services.AddAzureTableStorageService(storageConnectionString);

            // Step 4: Register Azure Blob Storage Factory as Singleton (shared client)
            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<AzureBlobStorageClientFactory>>();
                return new AzureBlobStorageClientFactory(storageConnectionString, logger);
            });

            // Step 5: Register named blob storage services for different containers
            // These use keyed services for proper DI resolution
            AddNamedBlobStorageService(services, petMediaContainer, "PetMedia");
            AddNamedBlobStorageService(services, shelterDocumentsContainer, "ShelterDocuments");
            AddNamedBlobStorageService(services, veterinaryMediaContainer, "VeterinaryMedia");
            AddNamedBlobStorageService(services, shelterMediaContainer, "ShelterMedia");

            // Step 6: Register default blob storage service for backward compatibility
            // This defaults to pet-media for components that don't specify a container
            services.AddScoped<IAzureBlobStorageService>(sp =>
            {
                var factory = sp.GetRequiredService<AzureBlobStorageClientFactory>();
                var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
                return new AzureBlobStorageService(factory, petMediaContainer, logger);
            });

            // Step 7: Register business services as Scoped
            services.AddBusinessServices();

            return services;
        }
    }
}
