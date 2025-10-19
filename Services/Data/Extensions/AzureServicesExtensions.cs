using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetAdoption.Services.Interfaces;
using Microsoft.Extensions.Logging;
using PetAdoption.Services.Data;

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
        /// Adds all business logic services as Scoped
        /// Business services should be scoped to ensure proper data isolation per request
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
        /// Orchestrates service registration in correct order with proper lifetimes
        /// </summary>
        public static async Task<IServiceCollection> AddAllAzureServicesAsync(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Validate configuration
            var keyVaultUri = configuration["KeyVault:VaultUri"];
            var secretName = configuration["KeyVault:AzureStorageConnectionStringSecret"];
            var containerName = configuration["Azure:BlobStorage:ContainerName"];

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

            if (string.IsNullOrWhiteSpace(containerName))
            {
                containerName = "pet-media";
            }

            // Step 1: Retrieve connection string from Key Vault (happens once at startup)
            var tempKeyVaultService = KeyVaultSecretService.CreateInstance(configuration);
            var storageConnectionString = await tempKeyVaultService.GetStorageConnectionStringAsync();

            // Step 2: Register KeyVaultSecretService as Singleton
            services.AddSingleton<KeyVaultSecretService>();

            // Step 3: Register Azure Table Storage as Singleton
            services.AddAzureTableStorageService(storageConnectionString);

            // Step 4: Register Azure Blob Storage (Factory as Singleton, Service as Scoped)
            services.AddAzureBlobStorageService(storageConnectionString, containerName);

            // Step 5: Register business services as Scoped
            services.AddBusinessServices();

            return services;
        }
    }
}
