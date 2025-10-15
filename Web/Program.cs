using Web.Components;
using Services.Data.Extensions;
using Services.Interfaces;

namespace Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Add Azure Table Storage with Key Vault integration
            builder.Services.AddAzureTableStorageWithKeyVault(builder.Configuration);

            var app = builder.Build();

            // Initialize tables at startup
            using (var scope = app.Services.CreateScope())
            {
                var tableInitService = scope.ServiceProvider.GetRequiredService<ITableInitializationService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    logger.LogInformation("Initializing Azure Table Storage tables...");
                    
                    // Set forceRecreate to true only during development if you want to reset tables
                    bool forceRecreate = app.Environment.IsDevelopment() && 
                                       builder.Configuration.GetValue<bool>("ForceRecreateTablesOnStartup", false);
                    
                    await tableInitService.InitializeTablesAsync(forceRecreate);
                    
                    logger.LogInformation("Table initialization completed successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize tables");
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
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
