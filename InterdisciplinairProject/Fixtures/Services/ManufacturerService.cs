using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace InterdisciplinairProject.Fixtures.Services
{
    public class ManufacturerService
    {
        private readonly string _rootDirectory;
        private readonly string _jsonPath;

        public ManufacturerService()
        {
            _rootDirectory = Path.Combine(
                             Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                             "InterdisciplinairProject",
                             "Fixtures");

            if (!Directory.Exists(_rootDirectory))
            {
                Directory.CreateDirectory(_rootDirectory);
            }

            Directory.CreateDirectory(_rootDirectory);

            _jsonPath = Path.Combine(_rootDirectory, "manufacturers.json");
        }

        // ========== JSON ==========
        private string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }

        public void SaveManufacturers(List<string> manufacturers)
        {
            var json = JsonSerializer.Serialize(manufacturers, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_jsonPath, json);
        }

        public List<string> LoadManufacturersFromJson()
        {
            if (!File.Exists(_jsonPath))
                return new List<string>();

            try
            {
                var json = File.ReadAllText(_jsonPath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        // ========== DIRECTORY ==========
        public List<string> GetManufacturers()
        {
            try
            {
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

        // ========== SAVE MANUFACTURER ==========
        public bool RegisterManufacturer(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string manufacturerName = Sanitize(name);

            List<string> manufacturers = LoadManufacturersFromJson();

            if (manufacturers.Any(m => m.Equals(manufacturerName, StringComparison.OrdinalIgnoreCase)))
                return false;

            try
            {
                // Create folder for this manufacturer
                Directory.CreateDirectory(Path.Combine(_rootDirectory, manufacturerName));

                // Add to the JSON list
                manufacturers.Add(manufacturerName);
                manufacturers.Sort(StringComparer.OrdinalIgnoreCase);

                SaveManufacturers(manufacturers);

                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}