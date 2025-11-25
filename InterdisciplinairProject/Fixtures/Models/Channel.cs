using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace InterdisciplinairProject.Fixtures.Models
{
    public class Channel : ObservableObject
    {
        private string _name = "";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _type = "";

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private string? _value;

        [JsonPropertyName("value")]
        public string? Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    if (int.TryParse(value, out int number))
                        Parameter = number;
                }
            }
        }

        private int _parameter;

        [JsonIgnore]
        public int Parameter
        {
            get => _parameter;
            set => SetProperty(ref _parameter, value);
        }

        [JsonPropertyName("min")]
        public int Min { get; set; } = 0;

        [JsonPropertyName("max")]
        public int Max { get; set; } = 255;

        [JsonPropertyName("time")]
        public int Time { get; set; } = 0;

        [JsonPropertyName("effectType")]
        public string effectType{ get; set; }

        [JsonIgnore]
        public ICommand? TestCommand { get; set; }
    }
}
