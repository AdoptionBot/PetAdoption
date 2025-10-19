using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetAdoption.Services.Interfaces;
using Microsoft.Extensions.Logging;
using PetAdoption.Services.Data;

namespace PetAdoption.Services.Data.Extensions
{
    /// <summary>
    /// Extensions for registering Azure services with dependency injection
    /// Includes Table Storage, Blob Storage, and Key Vault integration
    /// </summary>
    public static class AzureServicesExtensions
    {
        /// <summary>
        /// Adds Azure Table Storage service with a direct connection string (for testing)
        /// </summary>
        public static IServiceCollection AddAzureTableStorageService(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddSingleton<IAzureTableStorageService>(sp =>
                new AzureTableStorageService(connectionString));

            return services;
        }

        /// <summary>
        /// Adds Azure Blob Storage service with a direct connection string (for testing)
        /// </summary>
        public static IServiceCollection AddAzureBlobStorageService(
            this IServiceCollection services,
            string connectionString,
            string containerName)
        {
            services.AddSingleton<IAzureBlobStorageService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
                logger.LogInformation("Registering AzureBlobStorageService with connection string");
                
                try
                {
                    return new AzureBlobStorageService(connectionString, containerName, logger);
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "FATAL: Failed to create AzureBlobStorageService during DI registration");
                    throw;
                }
            });

            return services;
        }

        /// <summary>
        /// Adds all business logic services (User, Shelter, Pet)
        /// </summary>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IShelterService, ShelterService>();
            services.AddScoped<IPetService, PetService>();
            
            return services;
        }

        /// <summary>
        /// Adds all Azure services with Key Vault integration
        /// This method initializes services in the correct order to avoid deadlocks
        /// </summary>
        public static async Task<IServiceCollection> AddAllAzureServicesAsync(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Validate configuration
            var keyVaultUri = configuration["KeyVault:VaultUri"];
            var secretName = configuration["KeyVault:AzureStorageConnectionStringSecret"];

            if (string.IsNullOrEmpty(keyVaultUri))
            {
                throw new InvalidOperationException(
                    "KeyVault:VaultUri is not configured in appsettings.json");
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new InvalidOperationException(
                    "KeyVault:AzureStorageConnectionStringSecret is not configured in appsettings.json");
            }

            // Step 1: Create a temporary KeyVaultSecretService to retrieve connection string
            // This happens ONCE at startup, before any DI resolution
            var tempKeyVaultService = KeyVaultSecretService.CreateInstance(configuration);
            var storageConnectionString = await tempKeyVaultService.GetStorageConnectionStringAsync();
            var containerName = configuration["Azure:BlobStorage:ContainerName"] ?? "pet-media";

            // Step 2: Register KeyVaultSecretService in DI for runtime use
            services.AddSingleton<KeyVaultSecretService>();

            // Step 3: Register Azure Table Storage with the connection string
            services.AddAzureTableStorageService(storageConnectionString);

            // Step 4: Register Azure Blob Storage with the connection string
            services.AddAzureBlobStorageService(storageConnectionString, containerName);

            // Step 5: Register business services
            services.AddBusinessServices();

            return services;
        }
    }
}
