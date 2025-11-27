using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a fixture instance in a scene with its current channel values.
/// </summary>
public class Fixture : INotifyPropertyChanged
{
    private byte _dimmer;

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
        ChannelDescriptions = new Dictionary<string, string>();
        Channels = new ObservableCollection<Channel>();
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
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; }

    /// <summary>
    /// Gets or sets the description of the fixture.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the definition channels for this fixture.
    /// </summary>
    [JsonPropertyName("channels")]
    public ObservableCollection<Channel> Channels { get; set; } = new();

    /// <summary>
    /// Gets or sets the channel descriptions (e.g., "Ch1: Dimmer - General intensity").
    /// </summary>
    public Dictionary<string, string> ChannelDescriptions { get; set; }

    /// <summary>
    /// Gets or sets the channel types.
    /// </summary>
    public Dictionary<string, ChannelType> ChannelTypes { get; set; } = new();

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
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
