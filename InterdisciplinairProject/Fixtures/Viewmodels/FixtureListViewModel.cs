using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Converters;
using InterdisciplinairProject.Fixtures.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public enum SearchMode
    {
        Fixture,
        Manufacturer
    }

    public class FixtureListViewModel : INotifyPropertyChanged
    {
        private readonly string _fixturesFolder;
        private FileSystemWatcher? _watcher;

        public Array SearchModes => Enum.GetValues(typeof(SearchMode));

        public event EventHandler<string>? FixtureSelected;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ManufacturerGroup> ManufacturerGroups { get; set; } = new();

        private string _searchText = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    ApplySearch();
                }
            }
        }

        private SearchMode _selectedSearchMode = SearchMode.Fixture;

        public SearchMode SelectedSearchMode
        {
            get => _selectedSearchMode;
            set
            {
                if (_selectedSearchMode != value)
                {
                    _selectedSearchMode = value;
                    OnPropertyChanged(nameof(SelectedSearchMode));
                    ApplySearch();
                }
            }
        }

        private Fixture? _selectedFixture;
        public Fixture? SelectedFixture
        {
            get => _selectedFixture;
            set
            {
                if (_selectedFixture != value)
                {
                    _selectedFixture = value;
                    OnPropertyChanged(nameof(SelectedFixture));
                }
            }
        }

        public ICommand OpenFixtureCommand { get; }

        public FixtureListViewModel()
        {
            OpenFixtureCommand = new RelayCommand(OpenFixture);

            _fixturesFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "Fixtures");

            Directory.CreateDirectory(_fixturesFolder);

            ReloadFixturesFromFiles();
            StartWatchingDataFolder();
        }

        public void ReloadFixturesFromFiles()
        {
            ManufacturerGroups.Clear();

            if (!Directory.Exists(_fixturesFolder)) return;

            var allFixtures = new List<Fixture>();

            foreach (var file in Directory.GetFiles(_fixturesFolder, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var fixture = JsonSerializer.Deserialize<Fixture>(json);

                    if (fixture != null)
                    {
                        if (string.IsNullOrEmpty(fixture.Name))
                            fixture.Name = Path.GetFileNameWithoutExtension(file);
                        if (string.IsNullOrEmpty(fixture.Manufacturer))
                            fixture.Manufacturer = "Unknown";

                        if (!string.IsNullOrEmpty(fixture.ImageBase64))
                        {
                            fixture.ImageBase64 = ImageCompressionHelpers.DecompressBase64(fixture.ImageBase64);
                        }

                        allFixtures.Add(fixture);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load fixture from {file}: {ex.Message}");
                }
            }

            foreach (var group in allFixtures.GroupBy(f => f.Manufacturer))
            {
                var mg = new ManufacturerGroup
                {
                    Manufacturer = group.Key,
                    Fixtures = new ObservableCollection<Fixture>(group.OrderBy(f => f.Name))
                };
                mg.RefreshFilteredFixtures(SearchText);
                ManufacturerGroups.Add(mg);
            }

            ApplySearch();
        }

        private void ApplySearch()
        {
            if (SelectedSearchMode == SearchMode.Fixture)
            {
                foreach (var group in ManufacturerGroups)
                {
                    group.RefreshFilteredFixtures(SearchText);
                    group.IsVisible = group.FilteredFixtures.Count > 0;
                }
            }
            else // Manufacturer search
            {
                foreach (var group in ManufacturerGroups)
                {
                    group.IsVisible = string.IsNullOrWhiteSpace(SearchText)
                        || group.Manufacturer.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

                    group.RefreshFilteredFixtures(""); // show all fixtures in visible groups
                }
            }
        }

        // ------------------------------------------------------------
        // INotifyPropertyChanged
        // ------------------------------------------------------------
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // ------------------------------------------------------------
        // OPEN
        // ------------------------------------------------------------
        private void OpenFixture()
        {
            if (SelectedFixture == null) return;

            string filePath = Path.Combine(_fixturesFolder, SelectedFixture.Manufacturer, SelectedFixture.Name + ".json");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Fixture JSON-bestand niet gevonden.");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                FixtureSelected?.Invoke(this, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fout bij het laden van fixture: " + ex.Message);
            }
        }

        private void StartWatchingDataFolder()
        {
            if (!Directory.Exists(_fixturesFolder)) return;

            _watcher = new FileSystemWatcher(_fixturesFolder, "*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            };

            _watcher.Created += async (s, e) =>
            {
                await Task.Delay(200);
                string fileName = Path.GetFileNameWithoutExtension(e.Name);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!ManufacturerGroups.SelectMany(g => g.Fixtures).Any(f => f.Name == fileName))
                        ReloadFixturesFromFiles();
                });
            };

            _watcher.EnableRaisingEvents = true;
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
