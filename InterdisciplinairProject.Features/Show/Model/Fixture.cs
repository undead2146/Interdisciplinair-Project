using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Show.Model
{
    public class Fixture : INotifyPropertyChanged
    {
        private Dictionary<string, double> _channelRatios = new();

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
                
                // Apply dimmer proportionally to all channels
                ApplyDimmerToChannels();
                
                OnPropertyChanged(nameof(Dimmer));
            }
        }

        /// <summary>
        /// Calculates and stores the ratio of each channel value relative to the maximum channel value.
        /// This should be called after loading channel values from JSON.
        /// </summary>
        public void CalculateChannelRatios()
        {
            _channelRatios.Clear();

            if (Channels == null || Channels.Count == 0)
            {
                return;
            }

            // Find the maximum channel value
            byte maxValue = 0;
            foreach (var channel in Channels.Values)
            {
                if (channel.HasValue && channel.Value > maxValue)
                {
                    maxValue = channel.Value;
                }
            }

            // If all channels are zero, set dimmer to 0 and use equal ratios
            if (maxValue == 0)
            {
                _dimmer = 0;
                foreach (var channelName in Channels.Keys)
                {
                    _channelRatios[channelName] = 1.0;
                }
                return;
            }

            // Calculate ratio for each channel
            foreach (var channel in Channels)
            {
                byte channelValue = channel.Value ?? 0;
                _channelRatios[channel.Key] = channelValue / (double)maxValue;
            }

            // Set the dimmer to the maximum channel value found
            _dimmer = maxValue;
        }

        /// <summary>
        /// Applies the current dimmer value proportionally to all channels based on their stored ratios.
        /// </summary>
        private void ApplyDimmerToChannels()
        {
            if (Channels == null || _channelRatios.Count == 0)
            {
                // Fallback: just sync dimmer channel
                try
                {
                    Channels["dimmer"] = _dimmer;
                }
                catch
                {
                    // ignore if Channels not writable for some reason
                }
                return;
            }

            foreach (var channelName in Channels.Keys.ToList())
            {
                if (_channelRatios.TryGetValue(channelName, out double ratio))
                {
                    byte newValue = (byte)Math.Round(_dimmer * ratio);
                    Channels[channelName] = newValue;
                }
            }
        }
    }
}
