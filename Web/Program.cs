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

            // Retrieve authentication secrets from Key Vault
            var authSecrets = await KeyVaultSecretService.RetrieveAuthenticationSecretsAsync(builder.Configuration);

            // Register all Azure services (KeyVault, TableStorage, BlobStorage, Business Services)
            await builder.Services.AddAllAzureServicesAsync(builder.Configuration);

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

            var app = builder.Build();

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
    }
}
