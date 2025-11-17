using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;

namespace InterdisciplinairProject.Fixtures.Models
{
    public class Fixture
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;

        [JsonPropertyName("channels")]
        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();

        [JsonPropertyName("imageBase64")]
        public string ImageBase64 { get; set; } = string.Empty;

        // Property voor het aantal DMX-divisies (voor slider stapgrootte).

        [JsonPropertyName("dmxDivisions")]
        public int DmxDivisions { get; set; } = 255; // Standaard 255 (stap van 1)

        public Fixture(string name)
        {
            Name = name;
        }

        // Optioneel: Een parameterloze constructor voor de JsonSerializer
        public Fixture()
        {
            Name = string.Empty;
        }
    }
}