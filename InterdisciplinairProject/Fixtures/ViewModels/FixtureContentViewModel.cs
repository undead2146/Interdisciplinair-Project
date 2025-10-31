using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using System; // Toegevoegd voor EventHandler

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public partial class FixtureContentViewModel : ObservableObject
    {
        private string? _name;
        private string? _manufacturer; // NIEUW: Backing field voor de Fabrikant property

        public event EventHandler? DeleteRequested;

        public event EventHandler? BackRequested;

        public event EventHandler<FixtureContentViewModel>? EditRequested;

        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        // NIEUW: Publieke property voor Fabrikant (US 2, 3, 8)
        public string? Manufacturer
        {
            get => _manufacturer;
            set => SetProperty(ref _manufacturer, value);
        }

        public ObservableCollection<Channel> Channels { get; set; } = new();

        public ICommand BackCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand DeleteCommand { get; }

        public FixtureContentViewModel(string json)
        {
            BackCommand = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
            EditCommand = new RelayCommand(() => EditRequested?.Invoke(this, this));
            DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this, EventArgs.Empty));

            LoadFromJson(json);
        }

        private void LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<Fixture>(json, options);

            if (parsed != null)
            {
                Name = parsed.Name ?? string.Empty;

                // NIEUW: Laad de fabrikant uit het geparsete model
                Manufacturer = parsed.Manufacturer ?? "Custom";

                Channels.Clear();
                if (parsed.Channels != null)
                {
                    foreach (var c in parsed.Channels)
                        Channels.Add(c);
                }
            }
        }
    }
}