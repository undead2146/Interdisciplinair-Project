using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
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
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the channel.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the channel.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter of the channel.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the effect type.
    /// </summary>
    [JsonPropertyName("effectType")]
    public string effectType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test command.
    /// </summary>
    [JsonIgnore]
    public ICommand? TestCommand { get; set; }
}

/// <summary>
/// Represents a fixture instance in a scene with its current channel values.
/// </summary>
public class Fixture : INotifyPropertyChanged
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
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

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
    /// </summary>
    [JsonIgnore]
    public byte Dimmer
    {
        get => _dimmer;
        set
        {
            if (_dimmer == value) return;
            _dimmer = value;
            OnPropertyChanged(nameof(Dimmer));
        }
    }

    /// <summary>
    /// Gets or sets the DMX start address for this fixture (1-512).
    /// </summary>
    [JsonPropertyName("startAddress")]
    public int StartAddress { get; set; } = 1;

    /// <summary>
    /// Gets or sets the base64 encoded image for this fixture.
    /// </summary>
    [JsonPropertyName("imageBase64")]
    public string ImageBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the image path for this fixture.
    /// </summary>
    [JsonPropertyName("imagePath")]
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the definition channels for this fixture.
    /// </summary>
    [JsonPropertyName("channels")]
    public ObservableCollection<Channel> DefinitionChannels { get; set; } = new();

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
