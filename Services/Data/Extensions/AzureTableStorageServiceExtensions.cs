using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;

namespace Services.Data.Extensions
{
    /// <summary>
    /// Extensions for registering Azure Table Storage service with DI
    /// </summary>
    public static class AzureTableStorageServiceExtensions
    {
        public static IServiceCollection AddAzureTableStorageService(this IServiceCollection services)
        {
            services.AddSingleton<IAzureTableStorageService, AzureTableStorageService>();
            services.AddScoped<ITableInitializationService, TableInitializationService>();
            return services;
        }

        /// <summary>
        /// Adds Azure Table Storage service with Key Vault integration
        /// </summary>
        public static IServiceCollection AddAzureTableStorageWithKeyVault(this IServiceCollection services, IConfiguration configuration)
        {
            // Validate configuration
            var keyVaultUri = configuration["KeyVault:VaultUri"];
            var secretName = configuration["KeyVault:TableStorageConnectionStringSecret"];

            if (string.IsNullOrEmpty(keyVaultUri))
            {
                throw new InvalidOperationException("KeyVault:VaultUri is not configured in appsettings.json");
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new InvalidOperationException("KeyVault:TableStorageConnectionStringSecret is not configured in appsettings.json");
            }

            // Register the services
            services.AddSingleton<IAzureTableStorageService, AzureTableStorageService>();
            services.AddScoped<ITableInitializationService, TableInitializationService>();

            return services;
        }
    }
}
