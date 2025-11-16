using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InterdisciplinairProject.Fixtures.Models
{
    public class Fixture
    {
        // De 'Name' property, ik ga ervan uit dat deze niet direct geserialiseerd wordt.

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;


        // NIEUW: De property voor de fabrikant (US 2, 3, 8)
        // Deze is cruciaal voor het bewerken en opslaan van de fabrikantnaam.

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;

        // 🟢 TOEGEVOEGD: Property voor het aantal DMX-divisies (voor slider stapgrootte).
        [JsonPropertyName("dmxDivisions")]
        public int DmxDivisions { get; set; } = 255; // Standaard 255 (stap van 1)

        [JsonPropertyName("channels")]
        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();

        [JsonPropertyName("imagePath")]
        public string ImagePath { get; set; } = string.Empty;

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