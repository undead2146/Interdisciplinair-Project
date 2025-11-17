using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Show.Model
{
    public class Scene : INotifyPropertyChanged
    {
        private int _dimmer;

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

        [JsonPropertyName("fixtures")]
        public List<Fixture>? Fixtures { get; set; }

        [JsonIgnore]
        public string DisplayText => $"{Name} (ID: {Id}) - Dimmer: {Dimmer}%";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
