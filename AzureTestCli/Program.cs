using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace AzureTestCli;

public class TestAzureAuth
{
    public static async Task Main()
    {
        try
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: false)
                .Build();

            // Get Key Vault URI from configuration
            var keyVaultUri = configuration["KeyVault:VaultUri"];

            if (string.IsNullOrEmpty(keyVaultUri))
            {
                Console.WriteLine("❌ Error: KeyVault:VaultUri not found in appsettings.json");
                Console.WriteLine("Please add your Key Vault URI to appsettings.json");
                Console.WriteLine("\nExpected format in appsettings.json:");
                Console.WriteLine("{");
                Console.WriteLine("  \"KeyVault\": {");
                Console.WriteLine("    \"VaultUri\": \"https://your-keyvault-name.vault.azure.net/\",");
                Console.WriteLine("    \"TableStorageConnectionStringSecret\": \"TableStorageConnectionString\"");
                Console.WriteLine("  }");
                Console.WriteLine("}");
                return;
            }

            Console.WriteLine("Testing Azure Authentication from Visual Studio...");
            Console.WriteLine($"Key Vault URI from config: {keyVaultUri}");
            Console.WriteLine();

            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                Diagnostics = { IsLoggingEnabled = true }
            });

            var client = new SecretClient(new Uri(keyVaultUri), credential);

            Console.WriteLine("Attempting to list secrets...");
            Console.WriteLine();

            var secretCount = 0;
            await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync())
            {
                Console.WriteLine($"  ✓ Secret found: {secretProperties.Name}");
                secretCount++;
            }

            if (secretCount == 0)
            {
                Console.WriteLine("  No secrets found in Key Vault (but authentication worked!)");
            }

            Console.WriteLine();
            Console.WriteLine($"✅ Authentication successful! Found {secretCount} secret(s).");
        }
        catch (AuthenticationFailedException ex)
        {
            Console.WriteLine("❌ Authentication failed!");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Troubleshooting:");
            Console.WriteLine("1. Sign in to Visual Studio with your Azure account (top-right corner)");
            Console.WriteLine("2. Go to Tools → Options → Azure Service Authentication");
            Console.WriteLine("3. Verify the correct account is selected");
            Console.WriteLine("4. Ensure you have 'Key Vault Secrets User' role in Key Vault");
            Console.WriteLine("5. Wait 5-10 minutes after role assignment for propagation");
            Console.WriteLine();
            Console.WriteLine("Alternative: Run 'az login' in a terminal if using Azure CLI");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            Console.WriteLine("❌ Access Denied (403 Forbidden)");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("This means you're authenticated but don't have permission.");
            Console.WriteLine();
            Console.WriteLine("Solution:");
            Console.WriteLine("1. Go to Azure Portal → Your Key Vault");
            Console.WriteLine("2. Click 'Access control (IAM)' in left menu");
            Console.WriteLine("3. Click '+ Add' → 'Add role assignment'");
            Console.WriteLine("4. Select 'Key Vault Secrets User' role");
            Console.WriteLine("5. Select your account (SevaztianS)");
            Console.WriteLine("6. Complete the assignment");
            Console.WriteLine("7. Wait 5-10 minutes for propagation");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine("❌ Configuration file not found!");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Make sure 'appsettings.json' exists in your project.");
            Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine($"Full error details:");
            Console.WriteLine(ex.ToString());
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}