using System.Text.Json.Serialization;
using System.Windows.Input;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a channel in a fixture.
/// </summary>
public class Channel
{
    /// <summary>
    /// Gets or sets the name of the channel.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the channel.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the channel.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter of the channel.
    /// </summary>
    [JsonIgnore]
    public int Parameter { get; set; }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    [JsonPropertyName("min")]
    public int Min { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    [JsonPropertyName("max")]
    public int Max { get; set; } = 255;

    /// <summary>
    /// Gets or sets the time.
    /// </summary>
    [JsonPropertyName("time")]
    public int Time { get; set; } = 0;

    [JsonPropertyName("ranges")]
    public Dictionary<string, ChannelRange> Ranges { get; set; } = new();

    /// <summary>
    /// Gets or sets the effect type.
    /// </summary>
    [JsonPropertyName("channelEffect")]
    public ChannelEffect ChannelEffect { get; set; } = new ChannelEffect();

    /// <summary>
    /// Gets or sets the test command.
    /// </summary>
    [JsonIgnore]
    public ICommand? TestCommand { get; set; }
}
