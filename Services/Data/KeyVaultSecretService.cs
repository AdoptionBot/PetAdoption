using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PetAdoption.Services.Data.Models;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Service for retrieving secrets from Azure Key Vault
    /// Centralizes all Key Vault interactions for the application
    /// </summary>
    public class KeyVaultSecretService
    {
        private readonly SecretClient _secretClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeyVaultSecretService> _logger;

        public KeyVaultSecretService(IConfiguration configuration, ILogger<KeyVaultSecretService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var keyVaultUri = configuration["KeyVault:VaultUri"];
            if (string.IsNullOrEmpty(keyVaultUri))
            {
                throw new InvalidOperationException("KeyVault:VaultUri is not configured in appsettings.json");
            }

            _secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            _logger.LogInformation("KeyVaultSecretService initialized with vault: {VaultUri}", keyVaultUri);
        }

        /// <summary>
        /// Gets the SecretClient instance for direct access if needed
        /// </summary>
        public SecretClient SecretClient => _secretClient;

        /// <summary>
        /// Retrieves a secret value directly by its name in Key Vault
        /// </summary>
        /// <param name="secretName">The actual secret name in Key Vault</param>
        /// <returns>The secret value</returns>
        public async Task<string> GetSecretByNameAsync(string secretName)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
            }

            try
            {
                _logger.LogDebug("Retrieving secret: {SecretName}", secretName);
                var secret = await _secretClient.GetSecretAsync(secretName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Key Vault", secretName);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a secret from Key Vault based on the configuration key
        /// </summary>
        /// <param name="secretConfigKey">The configuration key for the secret name (e.g., "GoogleClientIdSecret")</param>
        /// <param name="fallbackValue">The fallback value if the secret cannot be retrieved</param>
        /// <returns>The secret value or fallback value</returns>
        public async Task<string> GetSecretAsync(string secretConfigKey, string fallbackValue = "")
        {
            var secretName = _configuration[$"KeyVault:{secretConfigKey}"];
            if (string.IsNullOrEmpty(secretName))
            {
                _logger.LogWarning("Secret configuration key KeyVault:{SecretConfigKey} not found in configuration", secretConfigKey);
                return fallbackValue;
            }

            try
            {
                var secret = await _secretClient.GetSecretAsync(secretName);
                _logger.LogDebug("Successfully retrieved secret for config key: {SecretConfigKey}", secretConfigKey);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve secret {SecretName} (config key: {SecretConfigKey}) from Key Vault. Using fallback value.", 
                    secretName, secretConfigKey);
                return fallbackValue;
            }
        }

        /// <summary>
        /// Retrieves multiple secrets from Key Vault based on configuration keys
        /// </summary>
        /// <param name="secretConfigKeys">Array of configuration keys</param>
        /// <returns>Dictionary mapping configuration keys to their secret values</returns>
        public async Task<Dictionary<string, string>> GetSecretsAsync(params string[] secretConfigKeys)
        {
            var secrets = new Dictionary<string, string>();
            
            foreach (var key in secretConfigKeys)
            {
                secrets[key] = await GetSecretAsync(key);
            }

            return secrets;
        }

        /// <summary>
        /// Retrieves the Azure Storage connection string from Key Vault
        /// This connection string is used for both Table Storage and Blob Storage
        /// </summary>
        /// <returns>The connection string</returns>
        public async Task<string> GetStorageConnectionStringAsync()
        {
            var secretName = _configuration["KeyVault:AzureStorageConnectionStringSecret"];
            if (string.IsNullOrEmpty(secretName))
            {
                throw new InvalidOperationException(
                    "KeyVault:AzureStorageConnectionStringSecret is not configured in appsettings.json");
            }

            return await GetSecretByNameAsync(secretName);
        }

        /// <summary>
        /// Retrieves all application secrets from Azure Key Vault
        /// Includes OAuth provider credentials and third-party API keys
        /// </summary>
        /// <returns>ApplicationSecrets object containing all application secrets</returns>
        public async Task<ApplicationSecrets> GetApplicationSecretsAsync()
        {
            _logger.LogInformation("Retrieving application secrets from Key Vault...");

            var secrets = new ApplicationSecrets
            {
                GoogleClientId = await GetSecretAsync("GoogleClientIdSecret"),
                GoogleClientSecret = await GetSecretAsync("GoogleClientSecretSecret"),
                MicrosoftClientId = await GetSecretAsync("MicrosoftClientIdSecret"),
                MicrosoftClientSecret = await GetSecretAsync("MicrosoftClientSecretSecret"),
                GoogleMapsApiKey = await GetSecretAsync("GoogleMapsApiKeySecret")
                //AppleClientId = await GetSecretAsync("AppleClientIdSecret"),
                //AppleTeamId = await GetSecretAsync("AppleTeamIdSecret"),
                //AppleKeyId = await GetSecretAsync("AppleKeyIdSecret"),
                //ApplePrivateKey = await GetSecretAsync("ApplePrivateKeySecret")
            };

            _logger.LogInformation("Application secrets retrieved successfully");

            return secrets;
        }

        /// <summary>
        /// Static factory method to create a KeyVaultSecretService instance without DI
        /// Useful for startup configuration scenarios where DI is not yet available
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <returns>KeyVaultSecretService instance</returns>
        public static KeyVaultSecretService CreateInstance(IConfiguration configuration)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<KeyVaultSecretService>();
            
            return new KeyVaultSecretService(configuration, logger);
        }

        /// <summary>
        /// Static method to retrieve application secrets without DI
        /// Useful for startup configuration scenarios where DI is not yet available
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <returns>ApplicationSecrets object</returns>
        public static async Task<ApplicationSecrets> RetrieveApplicationSecretsAsync(IConfiguration configuration)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<KeyVaultSecretService>();

            logger.LogInformation("Retrieving application secrets from Key Vault...");

            var keyVaultService = new KeyVaultSecretService(configuration, logger);
            var secrets = await keyVaultService.GetApplicationSecretsAsync();

            logger.LogInformation("Application secrets retrieved successfully");

            return secrets;
        }
    }
}