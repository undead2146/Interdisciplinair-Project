using System.ComponentModel;
using System.Text.Json.Serialization;

#pragma warning disable SA1600

namespace InterdisciplinairProject.Core.Models;

public class Scene : INotifyPropertyChanged
{
    private int _dimmer;
    private int _fadeInMs;
    private int _fadeOutMs;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("universe")]
    public int Universe { get; set; } = 1;

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

    [JsonPropertyName("fixtures")]
    public List<Fixture>? Fixtures { get; set; }

    [JsonIgnore]
    public string DisplayText => $"{Name} (ID: {Id}) - Dimmer: {Dimmer}%";

    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
