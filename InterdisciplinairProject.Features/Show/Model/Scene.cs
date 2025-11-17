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

        //NIEUWE FIELDS VOOR FADE
        private int _fadeInTime;
        private int _fadeOutTime;

        //Max limieten voor validatie (aanpasbaar indien nodig)
        public static int MaxFadeTimeMs { get; set; } = 60000; //60 seconden

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

        //FADE IN / OUT PROPERTIES
        [JsonPropertyName("fadeInTime")]
        public int FadeInTime
        {
            get => _fadeInTime;
            set
            {
                int validated = ValidateFade(value);
                if (_fadeInTime != validated)
                {
                    _fadeInTime = validated;
                    OnPropertyChanged();
                }
            }
        }

        [JsonPropertyName("fadeOutTime")]
        public int FadeOutTime
        {
            get => _fadeOutTime;
            set
            {
                int validated = ValidateFade(value);
                if (_fadeOutTime != validated)
                {
                    _fadeOutTime = validated;
                    OnPropertyChanged();
                }
            }
        }

        //DISPLAY TEXT
        [JsonIgnore]
        public string DisplayText =>
            $"{Name} (ID: {Id}) - Dimmer: {Dimmer}% - FadeIn: {FadeInTime}ms, FadeOut: {FadeOutTime}ms";

        //VALIDATIE METHODES
        private int ValidateFade(int value)
        {
            if (value < 0) return 0;
            if (value > MaxFadeTimeMs) return MaxFadeTimeMs;
            return value;
        }

        // SIMULATIE VAN FADE EFFECT
        //  (Deze methode kan je later koppelen aan output / DMX)
        public IEnumerable<(int timeMs, double fadeValue)> SimulateFadeIn()
        {
            if (FadeInTime <= 0)
            {
                yield return (0, 1.0);
                yield break;
            }

            int steps = 20;
            double stepTime = FadeInTime / (double)steps;

            for (int i = 0; i <= steps; i++)
            {
                double pct = i / (double)steps; // 0.0 → 1.0
                yield return ((int)(i * stepTime), pct);
            }
        }

        public IEnumerable<(int timeMs, double fadeValue)> SimulateFadeOut()
        {
            if (FadeOutTime <= 0)
            {
                yield return (0, 0.0);
                yield break;
            }

            int steps = 20;
            double stepTime = FadeOutTime / (double)steps;

            for (int i = 0; i <= steps; i++)
            {
                double pct = 1.0 - (i / (double)steps); // 1.0 → 0.0
                yield return ((int)(i * stepTime), pct);
            }
        }

        //  CONSTRUCTOR MET DEFAULT WAARDEN
        public Scene()
        {
            _fadeInTime = 0;
            _fadeOutTime = 0;
            _fixtures = new List<Fixture>();
        }

        //INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
