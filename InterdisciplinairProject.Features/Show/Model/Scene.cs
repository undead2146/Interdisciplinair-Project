using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Show.Model
{
    public class Scene : INotifyPropertyChanged
    {
        private int _dimmer;
        private int _fadeInMs;
        private int _fadeOutMs;

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
