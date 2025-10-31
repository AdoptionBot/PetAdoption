using Microsoft.AspNetCore.Authentication.Cookies;
using PetAdoption.Web.Components;
using PetAdoption.Services.Data.Extensions;
using PetAdoption.Services.Data;
using PetAdoption.Services;

namespace PetAdoption.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add Razor Components with Interactive Server
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Add controllers for authentication endpoints
            builder.Services.AddControllers();
            
            // Add authentication state and profile service
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<ProfileStateService>();

            // Retrieve application secrets from Key Vault
            var appSecrets = await KeyVaultSecretService.RetrieveApplicationSecretsAsync(builder.Configuration);

            // Store Google Maps API Key in configuration for easy access throughout the app
            builder.Configuration["GoogleMapsApiKey"] = appSecrets.GoogleMapsApiKey;

            // Register all Azure services (KeyVault, TableStorage, BlobStorage, Business Services)
            await builder.Services.AddAllAzureServicesAsync(builder.Configuration);

            // Register database seeder
            builder.Services.AddTransient<DatabaseSeeder>();

            // Configure authentication
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
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddGoogle(options =>
            {
                options.ClientId = appSecrets.GoogleClientId;
                options.ClientSecret = appSecrets.GoogleClientSecret;
                options.SaveTokens = true;
                options.CallbackPath = "/signin-google";
            })
            .AddMicrosoftAccount(options =>
            {
                options.ClientId = appSecrets.MicrosoftClientId;
                options.ClientSecret = appSecrets.MicrosoftClientSecret;
                options.SaveTokens = true;
                options.CallbackPath = "/signin-microsoft";
            });

            // Configure authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));
                
                options.AddPolicy("ShelterOnly", policy =>
                    policy.RequireRole("Shelter"));
            });

            // Configure SignalR with sensible defaults for Blazor Server
            builder.Services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB (reasonable for most scenarios)
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

            // Services for static asset versioning and JS module loading
            builder.Services.AddSingleton<IStaticAssetService, StaticAssetService>();
            builder.Services.AddScoped<IJsModuleService, JsModuleService>();

            var app = builder.Build();

            // Seed the database on startup
            await SeedDatabaseAsync(app);

            // Configure middleware pipeline
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
            app.UseRouting();
            
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();

            app.MapControllers();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }

        /// <summary>
        /// Seeds the database with initial data
        /// </summary>
        private static async Task SeedDatabaseAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Initializing database...");
                await seeder.SeedDatabaseAsync();
                logger.LogInformation("Database initialization completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database");
                // Don't throw - allow app to continue running
            }
        }
    }
}
