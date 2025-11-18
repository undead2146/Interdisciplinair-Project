using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Fixtures.Models
{
    public class Fixture
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;

        [JsonPropertyName("channels")]
        public ObservableCollection<Channel> Channels { get; set; } = new();

        [JsonPropertyName("imageBase64")]
        public string ImageBase64 { get; set; } = string.Empty;

        public Fixture() { }

        public Fixture(string name)
        {
            Name = name;
        }
    }
}
