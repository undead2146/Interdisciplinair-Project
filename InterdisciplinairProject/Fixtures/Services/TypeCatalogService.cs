using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace InterdisciplinairProject.Fixtures.Services
{
    public static class TypeCatalogService
    {
        private static List<TypeSpecification>? _specs;
        //private static readonly string DataPath =
        //    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "allTypes.json");
        //private static readonly string DataPath =
        //    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "Data", "allTypes.json");

        private static readonly string DataPath =
    Path.GetFullPath(
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..",        // up from bin\Debug\net8.0-windows to project root
            "Fixtures", "Data", "allTypes.json"));


        private static readonly List<TypeSpecification> _defaults = new()
        {
            new TypeSpecification{ name="Custom", input="custom"},
            new TypeSpecification{ name="testtest", input="slider", divisions=255},
            new TypeSpecification{ name="Lamp", input="slider", divisions=255},
            new TypeSpecification{ name="Star", input="slider", divisions=255},
            new TypeSpecification{ name="clock", input="slider", divisions=255},
            new TypeSpecification{ name="Tilt", input = "slider", divisions = 255},
            new TypeSpecification{ name="Ventilator", input = "slider", divisions = 255},
            new TypeSpecification{ name="Red", input="slider", divisions=255},
            new TypeSpecification{ name="Green", input="slider", divisions=255},
            new TypeSpecification{ name="Blue", input="slider", divisions=255},
            new TypeSpecification{ name="White", input="slider", divisions=255},
        };

        public static void EnsureLoaded()
        {
           

            if (_specs != null) return;
            try
            {
                if (File.Exists(DataPath))
                {
                    var json = File.ReadAllText(DataPath);
                    var arr = JsonSerializer.Deserialize<List<TypeSpecification>>(json);
                    _specs = (arr is { Count: > 0 }) ? arr : new List<TypeSpecification>(_defaults);
                }
                else
                {
                    _specs = new List<TypeSpecification>(_defaults);
                }
            }
            catch
            {
                _specs = new List<TypeSpecification>(_defaults);
            }
        }

        public static IReadOnlyList<string> Names
        {
            get { EnsureLoaded(); return _specs!.Select(s => s.name).ToList(); }
        }

        public static TypeSpecification? GetByName(string? name)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(name)) return null;
            return _specs!.FirstOrDefault(s => string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static void SaveToDisk()
        {
            try
            {
                var dir = Path.GetDirectoryName(DataPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_specs, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                File.WriteAllText(DataPath, json);
            }
            catch
            {
                MessageBox.Show("ERROR");
                // don’t throw; you can optionally log or MessageBox here if you want
            }
        }

        public static bool AddOrUpdate(TypeSpecification spec)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(spec.name)) return false;

            var existing = _specs!.FindIndex(s =>
                string.Equals(s.name, spec.name, StringComparison.OrdinalIgnoreCase));

            if (existing >= 0) _specs[existing] = spec;
            else _specs.Add(spec);

            SaveToDisk();
            return true;
        }

        // optional if you ever want a hard reload from disk
        public static void Reload()
        {
            _specs = null;
            EnsureLoaded();
        }
    }
}