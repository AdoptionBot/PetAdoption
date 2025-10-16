using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data.Extensions
{
    /// <summary>
    /// Extensions for registering Azure Table Storage services with dependency injection
    /// </summary>
    public static class AzureTableStorageServiceExtensions
    {
        /// <summary>
        /// Adds Azure Table Storage service with a direct connection string (for testing)
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">The Azure Table Storage connection string</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAzureTableStorageService(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddSingleton<IAzureTableStorageService>(sp =>
                new AzureTableStorageService(connectionString));
            
            services.AddScoped<ITableInitializationService, TableInitializationService>();

            return services;
        }

        /// <summary>
        /// Adds Azure Table Storage service with Key Vault integration for production use
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when Key Vault configuration is missing</exception>
        public static IServiceCollection AddAzureTableStorageWithKeyVault(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Validate configuration
            var keyVaultUri = configuration["KeyVault:VaultUri"];
            var secretName = configuration["KeyVault:TableStorageConnectionStringSecret"];

            if (string.IsNullOrEmpty(keyVaultUri))
            {
                throw new InvalidOperationException(
                    "KeyVault:VaultUri is not configured in appsettings.json");
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new InvalidOperationException(
                    "KeyVault:TableStorageConnectionStringSecret is not configured in appsettings.json");
            }

            // Register Key Vault Secret Service first (as it's a dependency for AzureTableStorageService)
            services.AddSingleton<KeyVaultSecretService>();

            // Register Azure Table Storage Service with Key Vault integration
            // This now depends on KeyVaultSecretService being registered first
            services.AddSingleton<IAzureTableStorageService, AzureTableStorageService>();

            // Register Table Initialization Service
            services.AddScoped<ITableInitializationService, TableInitializationService>();

            // Register User Service for authentication and user management
            services.AddScoped<IUserService, UserService>();

            return services;
        }

        /// <summary>
        /// Adds all Azure Table Storage related services
        /// This is a convenience method that registers all services needed for the application
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAllTableStorageServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.AddAzureTableStorageWithKeyVault(configuration);
        }
    }
}
