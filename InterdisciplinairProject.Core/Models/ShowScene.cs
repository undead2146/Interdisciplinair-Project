using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a scene in a show with observable properties.
/// </summary>
public class ShowScene : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="name">The name of the property that changed.</param>
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private int _dimmer;
    private int _fadeInMs;
    private int _fadeOutMs;

    /// <summary>
    /// Gets or sets the unique identifier of the scene.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the scene.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the dimmer value (0-100).
    /// </summary>
    [JsonPropertyName("dimmer")]
    public int Dimmer
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
    /// Gets or sets the fade-in duration in milliseconds.
    /// </summary>
    [JsonPropertyName("fadeInMs")]
    public int FadeInMs
    {
        get => _fadeInMs;
        set
        {
            if (_fadeInMs == value) return;
            _fadeInMs = value;
            OnPropertyChanged(nameof(FadeInMs));
        }
    }

    /// <summary>
    /// Gets or sets the fade-out duration in milliseconds.
    /// </summary>
    [JsonPropertyName("fadeOutMs")]
    public int FadeOutMs
    {
        get => _fadeOutMs;
        set
        {
            if (_fadeOutMs == value) return;
            _fadeOutMs = value;
            OnPropertyChanged(nameof(FadeOutMs));
        }
    }

    /// <summary>
    /// Gets or sets the fixtures in this scene.
    /// </summary>
    [JsonPropertyName("fixtures")]
    public List<Fixture>? Fixtures { get; set; }

    /// <summary>
    /// Gets the display text for this scene.
    /// </summary>
    [JsonIgnore]
    public string DisplayText => $"{Name} (ID: {Id}) - Dimmer: {Dimmer}%";
}