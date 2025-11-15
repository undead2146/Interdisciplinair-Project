using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Show.Model
{
    public class Scene : INotifyPropertyChanged
    {
        private string? _id;
        private string? _name;
        private int _dimmer;
        private List<Fixture>? _fixtures;

        [JsonPropertyName("id")]
        public string? Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        [JsonPropertyName("name")]
        public string? Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        [JsonPropertyName("dimmer")]
        public int Dimmer
        {
            get => _dimmer;
            set
            {
                if (_dimmer != value)
                {
                    _dimmer = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        [JsonPropertyName("fixtures")]
        public List<Fixture>? Fixtures
        {
            get => _fixtures;
            set
            {
                if (_fixtures != value)
                {
                    _fixtures = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public string DisplayText => $"{Name} (ID: {Id}) - Dimmer: {Dimmer}%";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
