using System.Globalization;

namespace PetAdoption.Services.Web
{
    /// <summary>
    /// Circuit-scoped service for managing culture within a Blazor Server circuit.
    /// Culture persistence is handled by reading the .AspNetCore.Culture cookie.
    /// </summary>
    public class LocalizationService
    {
        private const string DefaultCulture = "pt-PT";
        private CultureInfo _currentCulture;

        public event Action? OnCultureChanged;

        public LocalizationService()
        {
            // Initialize with current thread culture (set by RequestLocalization middleware)
            // The middleware reads the cookie and sets the thread culture before this runs
            _currentCulture = CultureInfo.CurrentCulture;
            
            // If still default after middleware, explicitly set it
            if (_currentCulture.Name == "en-US" && CultureInfo.CurrentUICulture.Name != "en-US")
            {
                _currentCulture = CultureInfo.CurrentUICulture;
            }
        }

        public CultureInfo CurrentCulture => _currentCulture;
        public string CurrentCultureName => _currentCulture.Name;

        public void SetCulture(string cultureName)
        {
            if (_currentCulture.Name != cultureName)
            {
                try
                {
                    _currentCulture = new CultureInfo(cultureName);

                    // Set culture for current thread and all future threads in this circuit
                    CultureInfo.CurrentCulture = _currentCulture;
                    CultureInfo.CurrentUICulture = _currentCulture;

                    OnCultureChanged?.Invoke();
                }
                catch (CultureNotFoundException)
                {
                    // Keep current culture if invalid
                }
            }
        }

        public bool IsPortuguese() => _currentCulture.Name.StartsWith("pt");
        public bool IsEnglish() => _currentCulture.Name.StartsWith("en");
    }
}