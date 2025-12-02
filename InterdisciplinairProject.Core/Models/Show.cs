using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a show containing multiple scenes.
/// </summary>
public class Show
{
    /// <summary>
    /// Gets or sets the unique identifier of the show.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the show.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the scenes in this show.
    /// </summary>
    [JsonPropertyName("scenes")]
    public List<Scene>? Scenes { get; set; }

    /// <summary>
    /// Gets the display text for this show.
    /// </summary>
    [JsonIgnore]
    public string DisplayText => $"{Name} (ID: {Id}) - # of Scenes: {Scenes?.Count() ?? 0}";
}