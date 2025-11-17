using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using InterdisciplinairProject.Fixtures.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public enum FilterMode
    {
        Fixture,
        Manufacturer
    }

    public class FixtureListViewModel : INotifyPropertyChanged
    {
        private readonly InterdisciplinairProject.Fixtures.Services.ManufacturerService _manufacturerService = new();
        private readonly string _fixturesFolder;
        private FileSystemWatcher? _watcher;

        public event EventHandler<string>? FixtureSelected;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Fixture> Fixtures { get; } = new();
        public ObservableCollection<ManufacturerGroup> ManufacturerGroups { get; set; } = new();

        // Master list to preserve all groups for search filtering
        private ObservableCollection<ManufacturerGroup> _allGroups = new();

        // NEW: expose FilterModes for the dropdown
        public ObservableCollection<FilterMode> FilterModes { get; } =
            new ObservableCollection<FilterMode>((FilterMode[])Enum.GetValues(typeof(FilterMode)));

        private string _searchText = "";
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

        private FilterMode _selectedFilterMode = FilterMode.Fixture;
        public FilterMode SelectedFilterMode
        {
            get => _selectedFilterMode;
            set
            {
                if (_selectedFilterMode != value)
                {
                    _selectedFilterMode = value;
                    OnPropertyChanged(nameof(SelectedFilterMode));
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
        public ICommand DeleteManufacturerCommand { get; }

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

            DeleteManufacturerCommand = new RelayCommand<string>(
                ExecuteDeleteManufacturer,
                CanExecuteDeleteManufacturer
            );
        }

        public void ReloadFixturesFromFiles()
        {
            ManufacturerGroups.Clear();
            _allGroups.Clear();

            if (!Directory.Exists(_fixturesFolder))
                return;

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

                        if (string.IsNullOrEmpty(fixture.ImagePath))
                        {
                            fixture.ImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "Views", "defaultFixturePng.png");
                        }
                        else
                        {
                            string fileName = Path.GetFileName(fixture.ImagePath);
                            string appDataImages = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "InterdisciplinairProject",
                                "Images"
                            );
                            fixture.ImagePath = Path.Combine(appDataImages, fileName);
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
                mg.RefreshFilteredFixtures(SearchText, SelectedFilterMode);
                ManufacturerGroups.Add(mg);
                _allGroups.Add(mg); // preserve master copy
            }
        }

        private void ApplySearch()
        {
            ManufacturerGroups.Clear();

            foreach (var group in _allGroups)
            {
                group.RefreshFilteredFixtures(SearchText, SelectedFilterMode);
                if (group.FilteredFixtures.Count > 0)
                    ManufacturerGroups.Add(group);
            }
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void OpenFixture()
        {
            if (SelectedFixture == null)
                return;

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

        private bool FixturesExists(string name) =>
            Fixtures.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

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
                    if (!FixturesExists(fileName))
                        Fixtures.Add(new Fixture(fileName));
                });
            };

            _watcher.EnableRaisingEvents = true;
        }

        private bool CanExecuteDeleteManufacturer(string? manufacturerName)
        {
            return !string.IsNullOrWhiteSpace(manufacturerName);
        }

        private void ExecuteDeleteManufacturer(string? manufacturerName)
        {
            if (string.IsNullOrWhiteSpace(manufacturerName)) return;

            MessageBoxResult result = MessageBox.Show(
                $"Weet u zeker dat u de fabrikant '{manufacturerName}' wilt verwijderen? Dit is enkel mogelijk als er geen fixtures onder bestaan.",
                "Fabrikant verwijderen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;

            bool success = _manufacturerService.DeleteManufacturer(manufacturerName);

            if (success)
            {
                MessageBox.Show($"Fabrikant '{manufacturerName}' succesvol verwijderd.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                ReloadFixturesFromFiles();
            }
            else
            {
                MessageBox.Show(
                    $"Fabrikant '{manufacturerName}' kon NIET worden verwijderd. Zorg ervoor dat er geen fixtures (bestanden) onder deze fabrikant bestaan.",
                    "Fout bij verwijderen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}
