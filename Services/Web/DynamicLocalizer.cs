using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace PetAdoption.Services.Web
{
    public class DynamicLocalizer<T>
    {
        private readonly LocalizationService _localizationService;
        private readonly IStringLocalizer<T> _localizer;
        private readonly ILogger<DynamicLocalizer<T>>? _logger;

        public DynamicLocalizer(
            IStringLocalizer<T> localizer,
            LocalizationService localizationService,
            ILogger<DynamicLocalizer<T>>? logger = null)
        {
            _localizer = localizer;
            _localizationService = localizationService;
            _logger = logger;
        }

        public string this[string name]
        {
            get
            {
                // Set culture before accessing localizer
                var culture = _localizationService.CurrentCulture;
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var result = _localizer[name];

                if (result == null || result.ResourceNotFound)
                {
                    _logger?.LogWarning(
                        "Resource NOT FOUND: Key={Key}, Culture={Culture}, Type={Type}",
                        name, culture.Name, typeof(T).Name);
                    return $"[{name}]";
                }

                return result.Value;
            }
        }

        public string this[string name, params object[] arguments]
        {
            get
            {
                var culture = _localizationService.CurrentCulture;
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var result = _localizer[name];

                if (result == null || result.ResourceNotFound)
                {
                    _logger?.LogWarning(
                        "Resource NOT FOUND: Key={Key}, Culture={Culture}, Type={Type}",
                        name, culture.Name, typeof(T).Name);
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