using InterdisciplinairProject.Fixtures.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Views;
using System.IO;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;
using System.ComponentModel;
using System.Collections.Generic;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public class FixtureListViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Fixture> Fixtures { get; } = new();

        public ICommand ImportFixtureCommand { get; }
        public ICommand ExportFixtureCommand { get; }
        public ICommand OpenFixtureCommand { get; }

        private readonly string _dataFolder;
        private FileSystemWatcher _watcher;

        public event EventHandler<string>? FixtureSelected;
        private Fixture? _selectedFixture;
        public Fixture? SelectedFixture 
        {
            //ook belangrijk voor wisselen van views!!!!
            //dus niet verwijderen
            get => _selectedFixture;
            set
            {
                if (_selectedFixture != value)
                {
                    _selectedFixture = value;
                    OnPropertyChanged(nameof(SelectedFixture));
                    (ExportFixtureCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        public FixtureListViewModel()
        {
            ImportFixtureCommand = new RelayCommand(ImportFixture);
            ExportFixtureCommand = new RelayCommand(ExportFixture, CanExportFixture);

            // Navigatie event triggeren
            OpenFixtureCommand = new RelayCommand(() =>
            {
                if (_selectedFixture != null)
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(_selectedFixture);
                    FixtureSelected?.Invoke(this, json);
                }
            });

            // Base directory: bin\Debug\net8.0-windows
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _dataFolder = Path.Combine(baseDir, "data");

            LoadFixturesFromFiles();
            StartWatchingDataFolder();
        }

        // ------------------------------------------------------------
        // Commands
        // ------------------------------------------------------------


        private void ImportFixture()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a Fixture JSON file",
                Filter = "JSON files (*.json)|*.json",
                InitialDirectory = _dataFolder
            };

            if (dialog.ShowDialog() != true)
                return;

            string jsonPath = dialog.FileName;

            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                JsonNode? root = JsonNode.Parse(jsonContent);

                if (root == null)
                {
                    System.Windows.MessageBox.Show("Invalid JSON structure.");
                    return;
                }

                // Allowed channel types
                var allowedTypes = new HashSet<string>
                {
                    "Lamp", "Ster", "Klok", "Ventilator", "Rood", "Groen", "Blauw", "Wit"
                };

                // Collect missing fields
                var missingFields = new List<string>();

                string? name = root["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                    missingFields.Add("'name'");

                string? manufacturer = root["manufacturer"]?.ToString();
                if (string.IsNullOrWhiteSpace(manufacturer))
                    missingFields.Add("'manufacturer'");

                JsonArray? channels = root["channels"] as JsonArray;
                if (channels == null || channels.Count == 0)
                {
                    missingFields.Add("'channels' array is missing or empty");
                }
                else
                {
                    for (int i = 0; i < channels.Count; i++)
                    {
                        if (channels[i] is JsonObject channel)
                        {
                            var missingInChannel = new List<string>();

                            string? chName = channel["Name"]?.ToString();
                            string? chType = channel["Type"]?.ToString();

                            if (string.IsNullOrWhiteSpace(chName))
                                missingInChannel.Add("Name");

                            if (string.IsNullOrWhiteSpace(chType))
                                missingInChannel.Add("Type");
                            else if (!allowedTypes.Contains(chType))
                                missingInChannel.Add($"Type ('{chType}' is invalid)");

                            if (missingInChannel.Count > 0)
                                missingFields.Add($"Channel {i + 1}: missing {string.Join(", ", missingInChannel)}");
                        }
                    }
                }

                if (missingFields.Count > 0)
                {
                    string message = "The following required fields are missing or invalid:\n- " + string.Join("\n- ", missingFields);
                    System.Windows.MessageBox.Show(message);
                    return; // Stop import because validation failed
                }

                // Check for duplicate fixture name
                if (FixturesExists(name!))
                {
                    System.Windows.MessageBox.Show($"Error importing fixture: Fixture '{name}' already exists");
                    return;
                }

                // Validation passed — copy file into data folder
                string targetFile = Path.Combine(_dataFolder, Path.GetFileName(jsonPath));
                if (!Directory.Exists(_dataFolder))
                    Directory.CreateDirectory(_dataFolder);

                File.Copy(jsonPath, targetFile, overwrite: true);

                Fixtures.Add(new Fixture(name!));

                System.Windows.MessageBox.Show($"Successfully imported fixture '{name}'.");
            }
            catch (JsonException)
            {
                System.Windows.MessageBox.Show("Error parsing JSON. Ensure the file is valid.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error importing fixture: " + ex.Message);
            }
        }

        private bool CanExportFixture() => SelectedFixture != null;

        private void ExportFixture()
        {
            if (SelectedFixture == null)
                return;

            string sourcePath = Path.Combine(_dataFolder, SelectedFixture.Name + ".json");
            if (!File.Exists(sourcePath))
            {
                System.Windows.MessageBox.Show("Fixture file not found: " + sourcePath);
                return;
            }

            try
            {
                // Load JSON
                string jsonContent = File.ReadAllText(sourcePath);
                JsonNode? root = JsonNode.Parse(jsonContent);
                if (root == null)
                {
                    System.Windows.MessageBox.Show("Invalid JSON file.");
                    return;
                }

                // Open pop-up to get new fixture name
                var exportWindow = new ExportFixtureWindow(SelectedFixture.Name)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (exportWindow.ShowDialog() == true)
                {
                    string newName = exportWindow.FixtureName;

                    // Update JSON "name" property
                    root["name"] = newName;

                    // Save file dialog
                    var saveDialog = new SaveFileDialog
                    {
                        Title = "Save exported fixture",
                        Filter = "JSON files (*.json)|*.json",
                        FileName = newName + ".json"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        File.WriteAllText(saveDialog.FileName, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                        System.Windows.MessageBox.Show($"Fixture exported successfully as '{newName}'!");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error exporting fixture: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // Fixture management
        // ------------------------------------------------------------
        private void LoadFixturesFromFiles()
        {
            if (!Directory.Exists(_dataFolder)) return;

            foreach (var file in Directory.GetFiles(_dataFolder, "*.json"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (!FixturesExists(fileName))
                    Fixtures.Add(new Fixture(fileName));
            }
        }

        private bool FixturesExists(string name)
        {
            foreach (var f in Fixtures)
            {

                if (f.Name == name)
                    return true;
            }
            return false;
        }

        // ------------------------------------------------------------
        // File system watcher
        // ------------------------------------------------------------
        private void StartWatchingDataFolder()
        {
            if (!Directory.Exists(_dataFolder)) return;

            _watcher = new FileSystemWatcher(_dataFolder, "*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
            };
            _watcher.Created += OnFileCreated;
            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(100);
            string fileName = Path.GetFileNameWithoutExtension(e.Name);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (!FixturesExists(fileName))
                    Fixtures.Add(new Fixture(fileName));
            });
        }

        // ------------------------------------------------------------
        // INotifyPropertyChanged
        // ------------------------------------------------------------
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
