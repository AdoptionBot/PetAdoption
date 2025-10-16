using System.Xml.Linq;

namespace Data.TableStorage.SchemaUtilities
{
    /// <summary>
    /// Loads and caches valid ICAO country designations from XML
    /// </summary>
    public static class IcaoCountryLoader
    {
        private static HashSet<string>? _validCountries;
        private static readonly object _lock = new();

        /// <summary>
        /// Gets all valid country designations from ICAO XML
        /// </summary>
        public static HashSet<string> GetValidCountries()
        {
            if (_validCountries != null)
                return _validCountries;

            lock (_lock)
            {
                if (_validCountries != null)
                    return _validCountries;

                _validCountries = LoadCountriesFromXml();
                return _validCountries;
            }
        }

        /// <summary>
        /// Checks if a country designation is valid
        /// </summary>
        public static bool IsValidCountry(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
                return false;

            var validCountries = GetValidCountries();
            return validCountries.Contains(country);
        }

        private static HashSet<string> LoadCountriesFromXml()
        {
            var countries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Get the path to the XML file
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var xmlPath = Path.Combine(baseDirectory, "XML", "ICAO_Codes_PT.xml");

                // Fallback paths for different deployment scenarios
                if (!File.Exists(xmlPath))
                {
                    xmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "XML", "ICAO_Codes_PT.xml");
                }

                if (!File.Exists(xmlPath))
                {
                    // Try relative path from project root
                    var projectRoot = Directory.GetCurrentDirectory();
                    while (projectRoot != null && !File.Exists(Path.Combine(projectRoot, "Data", "XML", "ICAO_Codes_PT.xml")))
                    {
                        projectRoot = Directory.GetParent(projectRoot)?.FullName;
                    }

                    if (projectRoot != null)
                    {
                        xmlPath = Path.Combine(projectRoot, "Data", "XML", "ICAO_Codes_PT.xml");
                    }
                }

                if (!File.Exists(xmlPath))
                {
                    throw new FileNotFoundException($"ICAO_Codes_PT.xml not found. Searched paths include: {xmlPath}");
                }

                var doc = XDocument.Load(xmlPath);
                var designacoes = doc.Descendants("Designacao")
                    .Select(x => x.Value.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                foreach (var designacao in designacoes)
                {
                    countries.Add(designacao);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load ICAO country codes from XML.", ex);
            }

            return countries;
        }

        /// <summary>
        /// Gets all valid countries as a list (useful for dropdowns)
        /// </summary>
        public static List<string> GetCountryList()
        {
            return GetValidCountries().OrderBy(c => c).ToList();
        }
    }
}