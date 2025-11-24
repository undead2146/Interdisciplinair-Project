using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents an effect that can be applied to a channel.
/// </summary>
public class ChannelEffect
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelEffect"/> class.
    /// </summary>
    public ChannelEffect()
    {
        EffectType = string.Empty;
    }

    /// <summary>
    /// Gets or sets the type of effect (e.g., "FadeIn", "FadeOut", "Strobe", etc.).
    /// </summary>
    [JsonPropertyName("effectType")]
    public string EffectType { get; set; }

    /// <summary>
    /// Gets or sets the duration of the effect in milliseconds.
    /// </summary>
    [JsonPropertyName("time")]
    public int Time { get; set; }

    /// <summary>
    /// Gets or sets the minimum value for the effect (0-255).
    /// </summary>
    [JsonPropertyName("min")]
    public byte Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for the effect (0-255).
    /// </summary>
    [JsonPropertyName("max")]
    public byte Max { get; set; }

    /// <summary>
    /// Gets or sets additional parameters for extensibility.
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}
