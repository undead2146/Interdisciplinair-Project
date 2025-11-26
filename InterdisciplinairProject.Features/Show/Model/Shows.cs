using System.Text.Json.Serialization;
using InterdisciplinairProject.Core.Models;

namespace Show.Model
{
    /// <summary>
    /// Represents a collection of scenes in a show.
    /// </summary>
    public class Shows
    {
        /// <summary>
        /// Gets or sets the unique identifier for the shows collection.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the shows collection.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the list of scenes in this shows collection.
        /// </summary>
        [JsonPropertyName("scenes")]
        public List<Scene>? Scenes { get; set; }

        /// <summary>
        /// Gets the display text for the shows collection.
        /// </summary>
        [JsonIgnore]
        public string DisplayText => $"{Name} (ID: {Id}) - # of Scenes: {Scenes?.Count ?? 0}";
    }
}
