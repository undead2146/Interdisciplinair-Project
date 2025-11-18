using System.Text.Json.Serialization;

namespace Show.Model
{
    public class Shows
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("scenes")]
        public List<Scene>? Scenes { get; set; }

        [JsonIgnore]
        public string DisplayText => $"{Name} (ID: {Id}) - # of Scenes: {Scenes.Count()}";
    }
}
