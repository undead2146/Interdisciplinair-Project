using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Show.Model
{
    public class Scene : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private int _universe;

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("universe")]
        public int Universe
        {
            get => _universe;
            set
            {
                if (_universe == value) return;
                _universe = value;
                OnPropertyChanged(nameof(Universe));
            }
        }



        [JsonPropertyName("fixtures")]
        public List<Fixture>? Fixtures { get; set; }

        [JsonIgnore]
        public string DisplayText => $"{Name} (ID: {Id}) ";

    }
}
