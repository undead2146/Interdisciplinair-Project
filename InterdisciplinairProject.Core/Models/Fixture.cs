using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a fixture instance in a scene with its current channel values.
/// </summary>
public class Fixture
{
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

    /// <summary>
    /// Gets or sets the dimmer channel value (0..255).
    /// </summary>
    public byte Dimmer { get; set; }

    /// <summary>
    /// Gets or sets the DMX start address for this fixture (1-512).
    /// </summary>
    [JsonPropertyName("startAddress")]
    public int StartAddress { get; set; } = 1;
}
