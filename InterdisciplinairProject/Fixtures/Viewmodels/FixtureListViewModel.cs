using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Fixtures.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public class FixtureListViewModel : INotifyPropertyChanged
    {
        private readonly string _fixturesFolder;
        private FileSystemWatcher? _watcher;

        public event EventHandler<string>? FixtureSelected;
        public event PropertyChangedEventHandler? PropertyChanged;

        public Array SearchModes => Enum.GetValues(typeof(SearchMode));
        public ObservableCollection<ManufacturerGroup> ManufacturerGroups { get; set; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                ApplySearch();
            }
        }

        public enum SearchMode
        {
            Fixture,
            Manufacturer
        }

        private SearchMode _selectedSearchMode = SearchMode.Fixture;
        public SearchMode SelectedSearchMode
        {
            get => _selectedSearchMode;
            set
            {
                _selectedSearchMode = value;
                OnPropertyChanged(nameof(SelectedSearchMode));
                ApplySearch();
            }
        }

        private Fixture? _selectedFixture;
        public Fixture? SelectedFixture
        {
            get => _selectedFixture;
            set
            {
                _selectedFixture = value;
                OnPropertyChanged(nameof(SelectedFixture));
            }
        }

        public ICommand OpenFixtureCommand { get; }
        public ICommand DeleteFixtureCommand { get; }

        public FixtureListViewModel()
        {
            OpenFixtureCommand = new RelayCommand(OpenFixture);
            DeleteFixtureCommand = new RelayCommand(DeleteSelected);

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
            var all = new List<Fixture>();

            foreach (var file in Directory.GetFiles(_fixturesFolder, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var fixture = JsonSerializer.Deserialize<Fixture>(json);
                    if (fixture == null) continue;

                    if (string.IsNullOrWhiteSpace(fixture.Name))
                        fixture.Name = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrWhiteSpace(fixture.Manufacturer))
                        fixture.Manufacturer = "Unknown";
                    if (!string.IsNullOrEmpty(fixture.ImageBase64))
                        fixture.ImageBase64 = ImageCompressionHelpers.DecompressBase64(fixture.ImageBase64);

                    all.Add(fixture);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            foreach (var g in all.GroupBy(f => f.Manufacturer))
            {
                var mg = new ManufacturerGroup
                {
                    Manufacturer = g.Key,
                    Fixtures = new ObservableCollection<Fixture>(g.OrderBy(f => f.Name))
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
                foreach (var g in ManufacturerGroups)
                {
                    g.RefreshFilteredFixtures(SearchText);
                    g.IsVisible = g.FilteredFixtures.Count > 0;
                }
            }
            else
            {
                foreach (var g in ManufacturerGroups)
                {
                    g.IsVisible = string.IsNullOrWhiteSpace(SearchText) ||
                                  g.Manufacturer.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                    g.RefreshFilteredFixtures("");
                }
            }
        }

        private void OpenFixture()
        {
            if (SelectedFixture == null) return;

            string path = Path.Combine(_fixturesFolder, SelectedFixture.Manufacturer, SelectedFixture.Name + ".json");

            if (!File.Exists(path))
            {
                MessageBox.Show("Fixture file not found.");
                return;
            }

            FixtureSelected?.Invoke(this, File.ReadAllText(path));
        }

        private void DeleteSelected()
        {
            if (SelectedFixture == null)
            {
                MessageBox.Show("No fixture selected.");
                return;
            }

            string msg = $"Delete fixture \"{SelectedFixture.Name}\"?";

            var confirm = MessageBox.Show(
                msg, "Delete fixture",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            string path = Path.Combine(_fixturesFolder, SelectedFixture.Manufacturer, SelectedFixture.Name + ".json");
            if (File.Exists(path))
                File.Delete(path);

            SelectedFixture = null;
            ReloadFixturesFromFiles();
        }

        private void StartWatchingDataFolder()
        {
            _watcher = new FileSystemWatcher(_fixturesFolder, "*.json");
            _watcher.Created += (s, e) => Application.Current.Dispatcher.Invoke(ReloadFixturesFromFiles);
            _watcher.Deleted += (s, e) => Application.Current.Dispatcher.Invoke(ReloadFixturesFromFiles);
            _watcher.EnableRaisingEvents = true;
        }

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
