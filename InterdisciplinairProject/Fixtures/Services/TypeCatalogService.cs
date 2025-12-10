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
        

        private static readonly List<TypeSpecification> _defaults = new()
        {
            new TypeSpecification { name = "Select a type",           input = "noInput"},
            new TypeSpecification { name = "Custom",           input = "custom"},
            // Base types (aligned with ChannelType, no Unknown)
            // For now: all 0–255
            new TypeSpecification { name = "Dimmer",           input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Red",              input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Green",            input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Blue",             input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "White",            input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Amber",            input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Strobe",           input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Pan",              input = "slider", min = 0, max = 360 },
            new TypeSpecification { name = "Tilt",             input = "slider", min = 0, max = 180 },
            new TypeSpecification { name = "ColorTemperature", input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Gobo",             input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Color",            input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Speed",            input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Pattern",          input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Power",            input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Rate",             input = "slider", min = 0, max = 255 },
            new TypeSpecification { name = "Brightness",       input = "slider", min = 0, max = 255 },
        };

      
        // In-memory catalog; no file, no allTypes.json
        private static List<TypeSpecification>? _specs = new(_defaults);

        /// <summary>
        /// Names for ComboBox ItemsSource.
        /// </summary>
        public static IReadOnlyList<string> Names =>
            _specs.Select(s => s.name).ToList();

        /// <summary>
        /// Get a type definition by name.
        /// </summary>
        public static TypeSpecification? GetByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return _specs.FirstOrDefault(s =>
                string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// In-memory add/update. No disk IO. 
        /// (Custom types you still use here for now if you want autocomplete inside this run.)
        /// </summary>
        public static bool AddOrUpdate(TypeSpecification spec)
        {
            if (string.IsNullOrWhiteSpace(spec.name)) return false;

            var existing = _specs.FindIndex(s =>
                string.Equals(s.name, spec.name, StringComparison.OrdinalIgnoreCase));

            if (existing >= 0)
                _specs[existing] = spec;
            else
                _specs.Add(spec);

            return true;
        }
    }
}