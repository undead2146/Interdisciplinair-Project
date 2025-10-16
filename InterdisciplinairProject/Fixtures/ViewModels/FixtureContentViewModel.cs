using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public partial class FixtureContentViewModel : ObservableObject
    {
        private string? _name;
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ObservableCollection<Channel> Channels { get; set; } = new();

        public ICommand BackCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public event EventHandler? DeleteRequested;
        public event EventHandler? BackRequested;
        public event EventHandler? EditRequested;

        public FixtureContentViewModel(string json)
        {
            BackCommand = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
            EditCommand = new RelayCommand(() => EditRequested?.Invoke(this, EventArgs.Empty));
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
