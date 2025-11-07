using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;

namespace PetAdoption.Services.Web
{
    public class DynamicLocalizer<T>
    {
        private readonly IStringLocalizerFactory _factory;
        private readonly LocalizationService _localizationService;
        private readonly IStringLocalizer _localizer;
        private readonly ILogger<DynamicLocalizer<T>>? _logger;

        public DynamicLocalizer(
            IStringLocalizerFactory factory,
            LocalizationService localizationService,
            ILogger<DynamicLocalizer<T>>? logger = null)
        {
            _factory = factory;
            _localizationService = localizationService;
            _logger = logger;

            // Get detailed type information
            var type = typeof(T);
            var assembly = type.Assembly;
            var assemblyName = assembly.GetName().Name;
            var fullTypeName = type.FullName ?? type.Name;

            _logger?.LogInformation(
                "=== DynamicLocalizer Constructor START ===\n" +
                "Type.Name: {TypeName}\n" +
                "Type.FullName: {FullTypeName}\n" +
                "Assembly.Name: {AssemblyName}\n" +
                "Assembly.Location: {AssemblyLocation}",
                type.Name, fullTypeName, assemblyName, assembly.Location);

            // List ALL embedded resources in the assembly
            var allResources = assembly.GetManifestResourceNames();
            _logger?.LogInformation(
                "=== Embedded Resources in Assembly ({Count} total) ===\n{Resources}",
                allResources.Length,
                string.Join("\n", allResources.Select((r, i) => $"  [{i}] {r}")));

            // Filter to show only .resources files (compiled resx)
            var resourceFiles = allResources.Where(r => r.EndsWith(".resources")).ToArray();
            _logger?.LogInformation(
                "=== Compiled Resource Files (.resources) ({Count} total) ===\n{Resources}",
                resourceFiles.Length,
                string.Join("\n", resourceFiles.Select((r, i) => $"  [{i}] {r}")));

            // Use the type directly - the factory will handle the ResourcesPath
            _localizer = _factory.Create(type);

            // Try to get the actual resource location through reflection
            try
            {
                var localizerType = _localizer.GetType();
                _logger?.LogInformation(
                    "=== Localizer Implementation ===\n" +
                    "Localizer Type: {LocalizerType}\n" +
                    "Localizer Assembly: {LocalizerAssembly}",
                    localizerType.FullName,
                    localizerType.Assembly.GetName().Name);

                // Try to inspect the ResourceManager if available
                var baseName = localizerType.GetProperty("BaseName", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(_localizer);
                var resourceManager = localizerType.GetProperty("ResourceManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(_localizer);

                if (baseName != null)
                {
                    _logger?.LogInformation("BaseName being used by localizer: {BaseName}", baseName);
                }

                if (resourceManager != null)
                {
                    var rmType = resourceManager.GetType();
                    var rmBaseName = rmType.GetProperty("BaseName")?.GetValue(resourceManager);
                    _logger?.LogInformation("ResourceManager.BaseName: {RMBaseName}", rmBaseName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to inspect localizer internals");
            }

            // Test resource lookup for common cultures
            var testCultures = new[] { "en-US", "pt-PT" };
            foreach (var cultureName in testCultures)
            {
                var testCulture = new CultureInfo(cultureName);

                // Set the culture temporarily
                var oldCulture = CultureInfo.CurrentCulture;
                var oldUICulture = CultureInfo.CurrentUICulture;

                try
                {
                    CultureInfo.CurrentCulture = testCulture;
                    CultureInfo.CurrentUICulture = testCulture;

                    // Try to get all strings for this culture
                    var allStrings = _localizer.GetAllStrings(includeParentCultures: false);
                    var stringCount = allStrings.Count();

                    _logger?.LogInformation(
                        "=== Resource Test for Culture: {Culture} ===\n" +
                        "Strings found: {Count}\n" +
                        "First 5 keys: {Keys}",
                        cultureName,
                        stringCount,
                        string.Join(", ", allStrings.Take(5).Select(s => s.Name)));
                }
                finally
                {
                    CultureInfo.CurrentCulture = oldCulture;
                    CultureInfo.CurrentUICulture = oldUICulture;
                }
            }

            _logger?.LogInformation("=== DynamicLocalizer Constructor END ===\n");
        }

        public string this[string name]
        {
            get
            {
                // Set culture before accessing localizer
                var culture = _localizationService.CurrentCulture;
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var result = _localizer.GetString(name);

                if (result.ResourceNotFound)
                {
                    _logger?.LogWarning(
                        "Resource NOT FOUND: Key={Key}, Culture={Culture}, Type={Type}, SearchedLocation={SearchedLocation}",
                        name, culture.Name, typeof(T).Name, result.SearchedLocation ?? "Unknown");
                }
                else
                {
                    _logger?.LogDebug(
                        "Resource FOUND: Key={Key}, Culture={Culture}, Value={Value}",
                        name, culture.Name, result.Value);
                }

                return result.ResourceNotFound ? $"[{name}]" : result.Value;
            }
        }

        public string this[string name, params object[] arguments]
        {
            get
            {
                var culture = _localizationService.CurrentCulture;
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var result = _localizer.GetString(name);

                if (result.ResourceNotFound)
                {
                    _logger?.LogWarning(
                        "Resource NOT FOUND (with args): Key={Key}, Culture={Culture}, Type={Type}, SearchedLocation={SearchedLocation}",
                        name, culture.Name, typeof(T).Name, result.SearchedLocation ?? "Unknown");
                    return $"[{name}]";
                }

                try
                {
                    return string.Format(result.Value, arguments);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to format resource string: Key={Key}, Value={Value}", name, result.Value);
                    return result.Value;
                }
            }
        }
    }
}