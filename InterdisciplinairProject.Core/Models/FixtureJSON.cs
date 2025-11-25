using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Fixtures.Models
{
    public class FixtureJSON
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;

        [JsonPropertyName("channels")]
        public ObservableCollection<Channel> Channels { get; set; } = new();

        [JsonPropertyName("imageBase64")]
        public string ImageBase64 { get; set; } = string.Empty;

        [JsonPropertyName("imagePath")]
        public string ImagePath { get; set; } = string.Empty;

        public FixtureJSON() { }

        public FixtureJSON(string name)
        {
            Name = name;
        }
    }
}