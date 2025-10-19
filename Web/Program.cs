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

            // Add services to the container
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Add controllers
            builder.Services.AddControllers();
            
            // Add authentication state
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<ProfileStateService>();

            // Retrieve authentication secrets (creates temporary KeyVaultSecretService)
            var authSecrets = await KeyVaultSecretService.RetrieveAuthenticationSecretsAsync(builder.Configuration);

            // Register ALL Azure services in one clean flow
            // This includes: KeyVault, TableStorage, BlobStorage, and business services
            await builder.Services.AddAllAzureServicesAsync(builder.Configuration);

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

            // Add authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));
            });

            // Configure SignalR for Blazor Server to handle large file uploads
            builder.Services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 52428800; // 50MB
                options.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
                options.HandshakeTimeout = TimeSpan.FromMinutes(5);
            });

            var app = builder.Build();

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
