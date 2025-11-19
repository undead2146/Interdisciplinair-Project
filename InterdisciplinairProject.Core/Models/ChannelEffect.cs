using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents an effect that can be applied to a fixture channel.
/// </summary>
public class ChannelEffect
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelEffect"/> class.
    /// </summary>
    public ChannelEffect()
    {
        EffectType = EffectType.FadeIn;
        Time = 0;
        Min = 0;
        Max = 255;
        Parameters = new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets or sets the type of effect.
    /// </summary>
    [JsonPropertyName("effectType")]
    public EffectType EffectType { get; set; }

    /// <summary>
    /// Gets or sets the time in milliseconds for the effect.
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
    /// Gets or sets additional parameters for the effect.
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; }
}

/// <summary>
/// Enumeration of effect types.
/// </summary>
public enum EffectType
{
    /// <summary>
    /// Fade in effect.
    /// </summary>
    FadeIn,

    /// <summary>
    /// Fade out effect.
    /// </summary>
    FadeOut,

    /// <summary>
    /// Strobe effect.
    /// </summary>
    Strobe,

    /// <summary>
    /// Pulse effect.
    /// </summary>
    Pulse,

    /// <summary>
    /// Custom effect.
    /// </summary>
    Custom
}
