using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a fixture instance in a scene with its current channel values.
/// </summary>
public class Fixture
{
    private Dictionary<string, double> _channelRatios = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Fixture"/> class.
    /// </summary>
    public Fixture()
    {
        FixtureId = string.Empty;
        InstanceId = string.Empty;
        Name = string.Empty;
        Manufacturer = string.Empty;
        Description = string.Empty;
        Channels = new Dictionary<string, byte?>();
        ChannelDescriptions = new Dictionary<string, string>();
        ChannelTypes = new Dictionary<string, ChannelType>();
        ChannelEffects = new Dictionary<string, List<ChannelEffect>>();
    }

    /// <summary>
    /// Gets or sets the unique identifier of the fixture type.
    /// </summary>
    [JsonPropertyName("fixtureId")]
    public string FixtureId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the fixture instance.
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; }

    /// <summary>
    /// Gets the unique identifier of the fixture instance (alias for InstanceId).
    /// </summary>
    public string Id => InstanceId;

    /// <summary>
    /// Gets or sets the display name of the fixture.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the manufacturer of the fixture.
    /// </summary>
    public string Manufacturer { get; set; }

    /// <summary>
    /// Gets or sets the description of the fixture.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the channels of the fixture with their current values.
    /// </summary>
    [JsonPropertyName("channels")]
    public Dictionary<string, byte?> Channels { get; set; }

    /// <summary>
    /// Gets or sets the channel descriptions (e.g., "Ch1: Dimmer - General intensity").
    /// </summary>
    public Dictionary<string, string> ChannelDescriptions { get; set; }

    /// <summary>
    /// Gets or sets the channel types.
    /// </summary>
    public Dictionary<string, ChannelType> ChannelTypes { get; set; }

    /// <summary>
    /// Gets or sets the channel effects (e.g., fade-in, fade-out per channel).
    /// Key is the channel name (e.g., "Ch1"), value is a list of effects.
    /// </summary>
    [JsonPropertyName("channelEffects")]
    public Dictionary<string, List<ChannelEffect>> ChannelEffects { get; set; }

    /// <summary>
    /// Gets the total number of channels in this fixture.
    /// </summary>
    public int ChannelCount => Channels?.Count ?? 0;

    /// <summary>
    /// Gets a value indicating whether this fixture is complex (more than 16 channels).
    /// </summary>
    public bool IsComplex => ChannelCount > 16;

    private byte _dimmer;

    /// <summary>
    /// Gets or sets the dimmer channel value (0..255).
    /// When set, it proportionally adjusts all channel values based on their initial ratios.
    /// </summary>
    public byte Dimmer
    {
        get => _dimmer;
        set
        {
            if (_dimmer != value)
            {
                _dimmer = value;
                ApplyDimmerToChannels();
            }
        }
    }

    /// <summary>
    /// Calculates and stores the ratio of each channel value relative to the maximum channel value.
    /// This should be called after loading channel values from JSON.
    /// </summary>
    public void CalculateChannelRatios()
    {
        _channelRatios.Clear();

        if (Channels == null || Channels.Count == 0)
        {
            return;
        }

        // Find the maximum channel value
        byte maxValue = 0;
        foreach (var channel in Channels.Values)
        {
            if (channel.HasValue && channel.Value > maxValue)
            {
                maxValue = channel.Value;
            }
        }

        // If all channels are zero, set dimmer to 0 and use equal ratios
        if (maxValue == 0)
        {
            _dimmer = 0;
            foreach (var channelName in Channels.Keys)
            {
                _channelRatios[channelName] = 1.0;
            }
            return;
        }

        // Calculate ratio for each channel
        foreach (var channel in Channels)
        {
            byte channelValue = channel.Value ?? 0;
            _channelRatios[channel.Key] = channelValue / (double)maxValue;
        }

        // Set the dimmer to the maximum channel value found
        _dimmer = maxValue;
    }

    /// <summary>
    /// Applies the current dimmer value proportionally to all channels based on their stored ratios.
    /// </summary>
    private void ApplyDimmerToChannels()
    {
        if (Channels == null || _channelRatios.Count == 0)
        {
            return;
        }

        foreach (var channelName in Channels.Keys.ToList())
        {
            if (_channelRatios.TryGetValue(channelName, out double ratio))
            {
                byte newValue = (byte)Math.Round(_dimmer * ratio);
                Channels[channelName] = newValue;
            }
        }
    }

    /// <summary>
    /// Updates a specific channel value and recalculates ratios to maintain proportional relationships.
    /// </summary>
    /// <param name="channelName">The name of the channel to update.</param>
    /// <param name="value">The new value for the channel.</param>
    public void UpdateChannelValue(string channelName, byte value)
    {
        if (Channels == null || !Channels.ContainsKey(channelName))
        {
            return;
        }

        Channels[channelName] = value;

        // Recalculate ratios and update dimmer based on the new maximum
        CalculateChannelRatios();
    }
}
