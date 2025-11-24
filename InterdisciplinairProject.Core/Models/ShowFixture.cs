using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a fixture instance in a show with observable properties.
/// </summary>
public class ShowFixture : INotifyPropertyChanged
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShowFixture"/> class.
    /// </summary>
    public ShowFixture()
    {
        Channels = new Dictionary<string, byte?>();
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="name">The name of the property that changed.</param>
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Gets or sets the unique identifier of the fixture type.
    /// </summary>
    [JsonPropertyName("fixtureId")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the fixture instance.
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the fixture.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the channels of the fixture with their current values.
    /// </summary>
    public Dictionary<string, byte?> Channels { get; set; }

    /// <summary>
    /// Gets or sets the dimmer channel value (0..255).
    /// </summary>
    private byte _dimmer;

    /// <summary>
    /// Gets or sets the dimmer channel value with change notification.
    /// </summary>
    [JsonIgnore]
    public byte Dimmer
    {
        get => _dimmer;
        set
        {
            if (_dimmer == value) return;
            _dimmer = value;
            // keep Channels in sync (non-breaking)
            try
            {
                Channels["dimmer"] = value;
            }
            catch
            {
                // ignore if Channels not writable for some reason
            }
            OnPropertyChanged(nameof(Dimmer));
        }
    }
}
