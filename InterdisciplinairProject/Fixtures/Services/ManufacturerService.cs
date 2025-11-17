using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows; // Gebruikt voor Environment.GetFolderPath

namespace InterdisciplinairProject.Fixtures.Services
{
    public class ManufacturerService
    {
        // Het root directory pad: Fixtures worden hier opgeslagen, met submappen per fabrikant
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
        /// (Voldoet aan requirement: folder wordt aangemaakt)
        /// </summary>
        public bool RegisterManufacturer(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string manufacturerName = name.Trim();

            // Controleer op bestaan (Voldoet aan requirement: mag niet al bestaan)
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
    }
}