using System.Xml.Linq;
using System.Reflection;

namespace PetAdoption.Data.TableStorage.Validation
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
                string? xmlPath = null;
                
                // Strategy 1: Look in the base directory (works for Azure App Service)
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var candidatePath = Path.Combine(baseDirectory, "XML", "ICAO_Codes_PT.xml");
                if (File.Exists(candidatePath))
                {
                    xmlPath = candidatePath;
                }

                // Strategy 2: Look in current directory
                if (xmlPath == null)
                {
                    candidatePath = Path.Combine(Directory.GetCurrentDirectory(), "XML", "ICAO_Codes_PT.xml");
                    if (File.Exists(candidatePath))
                    {
                        xmlPath = candidatePath;
                    }
                }

                // Strategy 3: Look relative to Data assembly location
                if (xmlPath == null)
                {
                    var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                    var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                    if (!string.IsNullOrEmpty(assemblyDirectory))
                    {
                        candidatePath = Path.Combine(assemblyDirectory, "XML", "ICAO_Codes_PT.xml");
                        if (File.Exists(candidatePath))
                        {
                            xmlPath = candidatePath;
                        }
                    }
                }

                // Strategy 4: Development fallback - walk up to find project root
                if (xmlPath == null)
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    while (currentDir != null)
                    {
                        candidatePath = Path.Combine(currentDir, "Data", "XML", "ICAO_Codes_PT.xml");
                        if (File.Exists(candidatePath))
                        {
                            xmlPath = candidatePath;
                            break;
                        }
                        currentDir = Directory.GetParent(currentDir)?.FullName;
                    }
                }

                if (string.IsNullOrEmpty(xmlPath) || !File.Exists(xmlPath))
                {
                    throw new FileNotFoundException(
                        $"ICAO_Codes_PT.xml not found. Searched locations:\n" +
                        $"  - {Path.Combine(baseDirectory, "XML", "ICAO_Codes_PT.xml")}\n" +
                        $"  - {Path.Combine(Directory.GetCurrentDirectory(), "XML", "ICAO_Codes_PT.xml")}\n" +
                        $"Please ensure the XML file is included in the deployment package.");
                }

                var doc = XDocument.Load(xmlPath);
                var designacoes = doc.Descendants("Designacao")
                    .Select(x => x.Value.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                foreach (var designacao in designacoes)
                {
                    countries.Add(designacao);
                }

                // Validate that we actually loaded countries
                if (countries.Count == 0)
                {
                    throw new InvalidOperationException("No countries were loaded from the XML file.");
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