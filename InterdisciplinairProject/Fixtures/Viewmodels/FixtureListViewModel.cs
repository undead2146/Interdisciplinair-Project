using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using InterdisciplinairProject.Fixtures.Views;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public class FixtureListViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Fixture> Fixtures { get; } = new();

        public ICommand ImportFixtureCommand { get; }
        public ICommand ExportFixtureCommand { get; }
        public ICommand OpenFixtureCommand { get; }

        private readonly string _fixturesFolder;
        private FileSystemWatcher? _watcher;

        public event EventHandler<string>? FixtureSelected;

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
                    (ExportFixtureCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        public FixtureListViewModel()
        {
            ImportFixtureCommand = new RelayCommand(ImportFixture);
            ExportFixtureCommand = new RelayCommand(ExportFixture, CanExportFixture);
            OpenFixtureCommand = new RelayCommand(OpenFixture);

            _fixturesFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "fixtures"
            );

            Directory.CreateDirectory(_fixturesFolder);

            ReloadFixturesFromFiles();
            StartWatchingDataFolder();
        }

        // ------------------------------------------------------------
        // IMPORT
        // ------------------------------------------------------------
        private void ImportFixture()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a Fixture JSON file",
                Filter = "JSON files (*.json)|*.json"
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

                // --- Validation ---
                var allowedTypes = new HashSet<string>
                {
                    "Lamp", "Ster", "Klok", "Ventilator", "Rood", "Groen", "Blauw", "Wit"
                };

                var missingFields = new List<string>();
                string? name = root["name"]?.ToString();
                string? manufacturer = root["manufacturer"]?.ToString();

                if (string.IsNullOrWhiteSpace(name))
                    missingFields.Add("'name'");
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
                    string message = "The following required fields are missing or invalid:\n- " +
                                     string.Join("\n- ", missingFields);
                    System.Windows.MessageBox.Show(message);
                    return;
                }

                if (string.IsNullOrWhiteSpace(name))
                    return;

                // --- Prevent duplicates ---
                string targetFile = Path.Combine(_fixturesFolder, name + ".json");
                if (FixturesExists(name) || File.Exists(targetFile))
                {
                    System.Windows.MessageBox.Show($"Error importing fixture: A fixture named '{name}' already exists.");
                    return;
                }

                // --- Copy to local folder ---
                File.Copy(jsonPath, targetFile, overwrite: false);
                Fixtures.Add(new Fixture(name));
                System.Windows.MessageBox.Show($"Successfully imported fixture '{name}'.");
            }
            catch (JsonException)
            {
                System.Windows.MessageBox.Show("Error parsing JSON. Ensure the file is valid.");
            }
            catch (IOException ioEx)
            {
                System.Windows.MessageBox.Show("File error: " + ioEx.Message);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error importing fixture: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // EXPORT
        // ------------------------------------------------------------
        private bool CanExportFixture() => SelectedFixture != null;

        private void ExportFixture()
        {
            if (SelectedFixture == null)
                return;

            string sourcePath = Path.Combine(_fixturesFolder, SelectedFixture.Name + ".json");
            if (!File.Exists(sourcePath))
            {
                System.Windows.MessageBox.Show("Fixture file not found: " + sourcePath);
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(sourcePath);
                JsonNode? root = JsonNode.Parse(jsonContent);

                if (root == null)
                {
                    System.Windows.MessageBox.Show("Invalid JSON file.");
                    return;
                }

                var exportWindow = new ExportFixtureWindow(SelectedFixture.Name)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (exportWindow.ShowDialog() == true)
                {
                    string newName = exportWindow.FixtureName.Trim();

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        System.Windows.MessageBox.Show("Fixture name cannot be empty.");
                        return;
                    }

                    root["name"] = newName;

                    var saveDialog = new SaveFileDialog
                    {
                        Title = "Save exported fixture",
                        Filter = "JSON files (*.json)|*.json",
                        FileName = newName + ".json"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        File.WriteAllText(saveDialog.FileName, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                        System.Windows.MessageBox.Show($"Fixture exported successfully as '{newName}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error exporting fixture: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // OPEN
        // ------------------------------------------------------------
        private void OpenFixture()
        {
            if (SelectedFixture == null)
                return;

            string filePath = Path.Combine(_fixturesFolder, SelectedFixture.Name + ".json");
            if (!File.Exists(filePath))
            {
                System.Windows.MessageBox.Show("Fixture JSON-bestand niet gevonden.");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                FixtureSelected?.Invoke(this, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Fout bij het laden van fixture: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // MANAGEMENT
        // ------------------------------------------------------------
        public void ReloadFixturesFromFiles()
        {
            Fixtures.Clear();
            if (!Directory.Exists(_fixturesFolder)) return;

            foreach (var file in Directory.GetFiles(_fixturesFolder, "*.json"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (!FixturesExists(fileName))
                    Fixtures.Add(new Fixture(fileName));
            }
        }

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
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            _watcher.Created += async (s, e) =>
            {
                await Task.Delay(200); // Allow file to finish writing
                string fileName = Path.GetFileNameWithoutExtension(e.Name);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!FixturesExists(fileName))
                        Fixtures.Add(new Fixture(fileName));
                });
            };

            _watcher.EnableRaisingEvents = true;
        }

        // ------------------------------------------------------------
        // INotifyPropertyChanged
        // ------------------------------------------------------------
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
