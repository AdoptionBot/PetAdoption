using System.Globalization;

namespace PetAdoption.Services.Web
{
    public class LocalizationService
    {
        private const string DefaultCulture = "pt-PT";
        private CultureInfo _currentCulture;

        public event Action? OnCultureChanged;

        public LocalizationService()
        {
            _currentCulture = new CultureInfo(DefaultCulture);
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
                    // Fallback to default if culture is invalid
                    _currentCulture = new CultureInfo(DefaultCulture);
                }
            }
        }

        public bool IsPortuguese() => _currentCulture.Name.StartsWith("pt");
        public bool IsEnglish() => _currentCulture.Name.StartsWith("en");
    }
}