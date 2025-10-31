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
        [JsonIgnore]
        public string Name { get; set; }

        // NIEUW: De property voor de fabrikant (US 2, 3, 8)
        // Deze is cruciaal voor het bewerken en opslaan van de fabrikantnaam.
        public string? Manufacturer { get; set; }

        [JsonPropertyName("channels")]
        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();

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