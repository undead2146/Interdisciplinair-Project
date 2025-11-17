using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows; // Gebruikt voor Environment.GetFolderPath

namespace InterdisciplinairProject.Fixtures.Services
{
    public class ManufacturerService
    {
        // Let op: deze _rootDirectory wijst naar ApplicationData (Roaming).
        // Dit is mogelijk niet de map waar de data wordt opgeslagen/gelezen,
        // maar wordt hier ongewijzigd gelaten zoals in uw originele code.
        private readonly string _rootDirectory = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "InterdisciplinairProject",
      "Fixtures"
    );

        public ManufacturerService()
        {
            // Zorg ervoor dat de root directory bestaat
            if (!Directory.Exists(_rootDirectory))
            {
                Directory.CreateDirectory(_rootDirectory);
            }
        }

        /// <summary>
        /// Haalt alle geregistreerde fabrikantnamen op uit de mapstructuur.
        /// </summary>
        public List<string> GetManufacturers()
        {
            try
            {
                // Haal alle submappen op en gebruik de mapnamen als fabrikantnamen
                return Directory.GetDirectories(_rootDirectory)
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .ToList()!;
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Registreert een nieuwe fabrikant en maakt de bijbehorende map aan.
        /// </summary>
        public bool RegisterManufacturer(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string manufacturerName = name.Trim();

            // Controleer op bestaan
            if (GetManufacturers().Any(m => m.Equals(manufacturerName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            try
            {
                string manufacturerPath = Path.Combine(_rootDirectory, manufacturerName);
                Directory.CreateDirectory(manufacturerPath); // Maak de map aan
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ---------------------------------------------------------------------
        // 🟢 NIEUWE METHODE (met correcte LocalApplicationData padlogica)
        // ---------------------------------------------------------------------
        /// <summary>
                /// Bepaalt het correcte LocalApplicationData pad en probeert de map te verwijderen.
                /// </summary>
        public bool TryDeleteManufacturerFolder(string manufacturerName)
        {
            // 1. Correcte Pad Bepalen (LocalApplicationData)
            string correctRootDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "InterdisciplinairProject",
        "Fixtures"
      );

            string safeName = SanitizeFileName(manufacturerName);
            string manufacturerDir = Path.Combine(correctRootDirectory, safeName);

            if (!Directory.Exists(manufacturerDir))
            {
                return true;
            }

            // 2. Probeer te verwijderen (alleen als de map leeg is)
            try
            {
                Directory.Delete(manufacturerDir, recursive: false);
                return true;
            }
            catch (IOException)
            {
                // De map is NIET LEEG.
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ---------------------------------------------------------------------
        // 🔴 ORIGINELE DELETE METHODE (AANGEPAST om de nieuwe logica aan te roepen)
        // ---------------------------------------------------------------------
        /// <summary>
        /// Verwijdert een fabrikant map. Dit is enkel mogelijk als de map leeg is.
        /// </summary>
        public bool DeleteManufacturer(string manufacturerName)
        {
            // Roep de nieuwe methode aan met de juiste padlogica
            return TryDeleteManufacturerFolder(manufacturerName);
        }
        // ---------------------------------------------------------------------

        // Helper methode om de naam veilig te maken voor het bestandssysteem
        public string SanitizeFileName(string input)
        {
            // Lijst van ongeldige karakters voor zowel bestands- als padnamen
            char[] invalidChars = Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()).ToArray();

            string cleanedName = input;

            // Vervang ongeldige karakters door een lege string
            foreach (char c in invalidChars)
            {
                cleanedName = cleanedName.Replace(c.ToString(), string.Empty);
            }

            // Verwijder spaties aan het begin en einde voor een schone mapnaam
            return cleanedName.Trim();
        }

    }
}