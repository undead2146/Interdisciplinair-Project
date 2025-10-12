using System.Text.Json.Serialization;

namespace Show.Model
{
    public class Scene
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("dimmer")]
        public int Dimmer { get; set; }

        [JsonPropertyName("fixtures")]
        public List<Fixture>? Fixtures { get; set; }

        [JsonIgnore]
        public string DisplayText => $"{Name} (ID: {Id}) - Dimmer: {Dimmer}%";
    }
}
