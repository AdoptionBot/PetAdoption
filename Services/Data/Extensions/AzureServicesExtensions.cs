using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetAdoption.Services.Interfaces;

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
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">The Azure Storage connection string</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAzureTableStorageService(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddSingleton<IAzureTableStorageService>(sp =>
                new AzureTableStorageService(connectionString));

            return services;
        }

        /// <summary>
        /// Adds Azure Storage services (Table and Blob) with Key Vault integration for production use
        /// Both services use the same storage account connection string
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when Key Vault configuration is missing</exception>
        public static IServiceCollection AddAzureStorageWithKeyVault(
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

            // Register Key Vault Secret Service first (shared dependency)
            services.AddSingleton<KeyVaultSecretService>();

            // Register Azure Table Storage Service with Key Vault integration
            services.AddSingleton<IAzureTableStorageService, AzureTableStorageService>();

            // Register Azure Blob Storage Service with Key Vault integration
            // Uses the same storage account connection string as Table Storage
            services.AddSingleton<IAzureBlobStorageService, AzureBlobStorageService>();

            // Register User Service for authentication and user management
            services.AddScoped<IUserService, UserService>();

            return services;
        }

        /// <summary>
        /// Adds Azure Blob Storage service with a direct connection string (for testing)
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">The Azure Storage connection string</param>
        /// <param name="containerName">The container name (defaults to "pet-media")</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAzureBlobStorageService(
            this IServiceCollection services,
            string connectionString,
            string containerName = "pet-media")
        {
            services.AddSingleton<IAzureBlobStorageService>(sp =>
                new AzureBlobStorageService(connectionString, containerName));

            return services;
        }

        /// <summary>
        /// Adds all Azure services (Table Storage, Blob Storage, Key Vault integration)
        /// This is a convenience method that registers all services needed for the application
        /// Both Table Storage and Blob Storage use the same storage account connection string
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAllAzureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.AddAzureStorageWithKeyVault(configuration);
        }

        #region Obsolete Methods (for backward compatibility)

        /// <summary>
        /// Adds Azure Table Storage service with Key Vault integration for production use
        /// </summary>
        [Obsolete("Use AddAzureStorageWithKeyVault() instead, which registers both Table and Blob Storage.")]
        public static IServiceCollection AddAzureTableStorageWithKeyVault(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.AddAzureStorageWithKeyVault(configuration);
        }

        /// <summary>
        /// Adds Azure Blob Storage service with Key Vault integration for production use
        /// </summary>
        [Obsolete("Use AddAzureStorageWithKeyVault() instead, which registers both Table and Blob Storage.")]
        public static IServiceCollection AddAzureBlobStorageWithKeyVault(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.AddAzureStorageWithKeyVault(configuration);
        }

        #endregion
    }
}
