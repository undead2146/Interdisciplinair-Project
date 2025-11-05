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
using System.Windows.Media.Imaging;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public class FixtureListViewModel : INotifyPropertyChanged
    {
        private readonly string _fixturesFolder;
        private FileSystemWatcher? _watcher;

        public event EventHandler<string>? FixtureSelected;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Fixture> Fixtures { get; } = new();
        public ObservableCollection<ManufacturerGroup> ManufacturerGroups { get; set; } = new();

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
                    foreach (var group in ManufacturerGroups)
                        group.RefreshFilteredFixtures(_searchText);
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

                        // image decompressie
                        if (!string.IsNullOrEmpty(fixture.ImageBase64))
                        {
                            byte[] compressedBytes = Convert.FromBase64String(fixture.ImageBase64);

                            using (var compressedStream = new MemoryStream(compressedBytes))
                            using (var gzip = new System.IO.Compression.GZipStream(compressedStream, System.IO.Compression.CompressionMode.Decompress))
                            using (var ms = new MemoryStream())
                            {
                                gzip.CopyTo(ms);
                                ms.Position = 0;

                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.StreamSource = ms;
                                bitmap.EndInit();
                                bitmap.Freeze();

                                fixture.ImageSource = bitmap;
                            }
                        }
                        else
                        {
                            // fallback naar default image
                            var bitmap = new BitmapImage(new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "Views", "defaultFixturePng.png")));
                            fixture.ImageSource = bitmap;
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

        // ------------------------------------------------------------
        // MANAGEMENT
        // ------------------------------------------------------------
        private bool FixturesExists(string name) =>
            Fixtures.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        // ------------------------------------------------------------
        // WATCHER
        // ------------------------------------------------------------
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
    }
}
