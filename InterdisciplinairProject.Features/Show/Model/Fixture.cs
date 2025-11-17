using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Show.Model
{
    public class Fixture : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        [JsonPropertyName("fixtureId")]
        public string? Id { get; set; }

        [JsonPropertyName("instanceId")]
        public string? InstanceId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        // keep existing dictionary for compatibility with existing serialized data
        public Dictionary<string, byte?> Channels { get; set; } = new();

        // Simple observable convenience property for the dimmer channel (0..255)
        private byte _dimmer;
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
}
