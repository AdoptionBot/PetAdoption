using System.Reflection;

namespace PetAdoption.Services;

public interface IStaticAssetService
{
    string GetVersionedUrl(string assetPath);
    string GetVersion();
}

public class StaticAssetService : IStaticAssetService
{
    private static readonly string _version = GenerateVersion();

    private static string GenerateVersion()
    {
        // Use assembly build time as version - this changes with each deployment
        var assembly = Assembly.GetExecutingAssembly();
        var buildDate = new FileInfo(assembly.Location).LastWriteTimeUtc;
        return buildDate.Ticks.ToString();
    }

    public string GetVersion()
    {
        return _version;
    }

    public string GetVersionedUrl(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return assetPath;

        // Handle absolute URLs (external resources)
        if (assetPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            assetPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            assetPath.StartsWith("//", StringComparison.OrdinalIgnoreCase))
        {
            return assetPath; // Don't version external resources
        }

        // Add version query parameter
        var separator = assetPath.Contains('?') ? "&" : "?";
        return $"{assetPath}{separator}v={_version}";
    }
}