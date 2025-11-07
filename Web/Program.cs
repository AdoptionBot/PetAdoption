using Microsoft.AspNetCore.Authentication.Cookies;
using PetAdoption.Web.Components;
using PetAdoption.Services.Data.Extensions;
using PetAdoption.Services.Data;
using PetAdoption.Services.Web;
using PetAdoption.Services;
using System.Globalization;


namespace PetAdoption.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Enable detailed logging for localization (temporary for debugging)
            builder.Logging.AddFilter("PetAdoption.Services.Web.DynamicLocalizer", LogLevel.Information);
            builder.Logging.AddFilter("Microsoft.Extensions.Web.Localization", LogLevel.Debug);

            // Add Razor Components with Interactive Server
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Register HttpClient for dependency injection
            builder.Services.AddHttpClient();

            // Add controllers for authentication endpoints
            builder.Services.AddControllers();
            
            // Add authentication state and profile service
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<ProfileStateService>();

            // Add Localization with correct ResourcesPath
            builder.Services.AddLocalization(options => 
            {
                options.ResourcesPath = "Resources";
            });
            
            // Register LocalizationService as Scoped (per circuit in Blazor Server)
            builder.Services.AddScoped<LocalizationService>();
            
            // Register DynamicLocalizer as Scoped
            builder.Services.AddScoped(typeof(DynamicLocalizer<>));

            // Configure supported cultures
            var supportedCultures = new[]
            {
                new CultureInfo("pt-PT"),
                new CultureInfo("en-US")
            };

            // Retrieve application secrets from Key Vault
            var appSecrets = await KeyVaultSecretService.RetrieveApplicationSecretsAsync(builder.Configuration);

            // Store unified Google API Key in configuration
            builder.Configuration["GoogleApiKey"] = appSecrets.GoogleApiKey;

            // Register all Azure services
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

            // Configure SignalR
            builder.Services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
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
            }
        }
    }
}
