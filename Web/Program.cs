using Microsoft.AspNetCore.Authentication.Cookies;
using PetAdoption.Web.Components;
using PetAdoption.Services.Data.Extensions;
using PetAdoption.Services.Data;
using PetAdoption.Services.Interfaces;
using PetAdoption.Services;

namespace PetAdoption.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Add controllers for auth endpoints
            builder.Services.AddControllers();

            // Add cascading authentication state
            builder.Services.AddCascadingAuthenticationState();

            // Register ProfileStateService as a scoped service
            builder.Services.AddScoped<ProfileStateService>();

            // Add shelter service
            builder.Services.AddScoped<IShelterService, ShelterService>();

            // Add pet service
            builder.Services.AddScoped<IPetService, PetService>();

            // Retrieve authentication secrets BEFORE registering services
            var authSecrets = await KeyVaultSecretService.RetrieveAuthenticationSecretsAsync(builder.Configuration);

            // Add Azure Table Storage with Key Vault integration
            // This registers: KeyVaultSecretService, IAzureTableStorageService, ITableInitializationService, IUserService
            builder.Services.AddAzureTableStorageWithKeyVault(builder.Configuration);

            // Configure authentication with retrieved secrets
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/api/auth/logout";
                options.AccessDeniedPath = "/access-denied";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
            })
            .AddGoogle(options =>
            {
                options.ClientId = authSecrets.GoogleClientId;
                options.ClientSecret = authSecrets.GoogleClientSecret;
                options.SaveTokens = true;
                options.CallbackPath = "/signin-google";
            })
            .AddMicrosoftAccount(options =>
            {
                options.ClientId = authSecrets.MicrosoftClientId;
                options.ClientSecret = authSecrets.MicrosoftClientSecret;
                options.SaveTokens = true;
                options.CallbackPath = "/signin-microsoft";
            });
            //.AddApple(options =>
            //{
            //    options.ClientId = authSecrets.AppleClientId;
            //    options.TeamId = authSecrets.AppleTeamId;
            //    options.KeyId = authSecrets.AppleKeyId;
                
            //    // Use private key from Key Vault
            //    if (!string.IsNullOrEmpty(authSecrets.ApplePrivateKey))
            //    {
            //        // Handle escaped newlines and create temporary file
            //        var formattedKey = authSecrets.ApplePrivateKey.Replace("\\n", "\n");
            //        var tempKeyFile = Path.Combine(Path.GetTempPath(), $"apple_key_{Guid.NewGuid()}.p8");
            //        File.WriteAllText(tempKeyFile, formattedKey);
                    
            //        options.UsePrivateKey(keyId => 
            //            new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.GetTempPath())
            //                .GetFileInfo(Path.GetFileName(tempKeyFile)));
            //    }
                
            //    options.SaveTokens = true;
            //    options.CallbackPath = "/signin-apple-callback";
            //});

            // Add authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));
            });

            var app = builder.Build();

            // Initialize tables at startup
            using (var scope = app.Services.CreateScope())
            {
                var tableInitService = scope.ServiceProvider.GetRequiredService<ITableInitializationService>();
                var appLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    appLogger.LogInformation("Initializing Azure Table Storage tables...");
                    
                    bool forceRecreate = app.Environment.IsDevelopment() && 
                                       builder.Configuration.GetValue<bool>("ForceRecreateTablesOnStartup", false);
                    
                    await tableInitService.InitializeTablesAsync(forceRecreate);
                    
                    appLogger.LogInformation("Table initialization completed successfully");
                }
                catch (Exception ex)
                {
                    appLogger.LogError(ex, "Failed to initialize tables");
                    throw;
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Add authentication and authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseAntiforgery();

            // Map controllers for auth endpoints
            app.MapControllers();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
