using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using InterdisciplinairProject.Fixtures.Views;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public partial class MainWindowFixturesViewModel : ObservableObject
    {
        private readonly FixtureListViewModel fixtureListVm;
        private readonly string _fixturesFolder;

        // Track currently selected fixture
        private Fixture? _selectedFixture;

        [ObservableProperty]
        private object currentViewModel;

        public event EventHandler? DeleteRequested;

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

        public ICommand CreateFixtureCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand ImportFixtureCommand { get; }

        public ICommand ExportFixtureCommand { get; }

        public MainWindowFixturesViewModel()
        {
            CreateFixtureCommand = new RelayCommand(CreateFixture);
            DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this, EventArgs.Empty));
            ImportFixtureCommand = new RelayCommand(ImportFixture);
            ExportFixtureCommand = new RelayCommand(ExportFixture, CanExportFixture);

            fixtureListVm = new FixtureListViewModel();
            fixtureListVm.FixtureSelected += OnFixtureSelected;

            // Sync SelectedFixture between both viewmodels
            fixtureListVm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(fixtureListVm.SelectedFixture))
                    SelectedFixture = fixtureListVm.SelectedFixture;
            };

            // Folder for fixtures (matches FixtureListViewModel)
            _fixturesFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "Fixtures");

            Directory.CreateDirectory(_fixturesFolder);

            CurrentViewModel = fixtureListVm;
        }

        // ------------------------------------------------------------
        // VIEW NAVIGATION
        // ------------------------------------------------------------
        private void OnFixtureSelected(object? sender, string json)
        {
            var detailVm = new FixtureContentViewModel(json);
            detailVm.BackRequested += (_, __) => CurrentViewModel = fixtureListVm;
            detailVm.DeleteRequested += (_, __) => OnFixtureDelete(detailVm.Name, detailVm.Manufacturer);
            detailVm.EditRequested += (_, contentVm) =>
            {
                var editVm = new FixtureCreateViewModel(contentVm);
                editVm.BackRequested += (_, __) => CurrentViewModel = fixtureListVm;
                editVm.FixtureSaved += (_, __) =>
                {
                    fixtureListVm.ReloadFixturesFromFiles();
                    CurrentViewModel = fixtureListVm;
                };

                CurrentViewModel = editVm;
            };

            CurrentViewModel = detailVm;
        }

        private void CreateFixture()
        {
            var createVm = new FixtureCreateViewModel();

            createVm.BackRequested += (_, __) =>
            {
                fixtureListVm.ReloadFixturesFromFiles();
                CurrentViewModel = fixtureListVm;
            };

            CurrentViewModel = createVm;
        }

        private void OnFixtureDelete(string fixtureName, string manufacturerName)
        {
            string filePath = Path.Combine(_fixturesFolder,manufacturerName, fixtureName + ".json");

            if (!File.Exists(filePath))
            {
                System.Windows.MessageBox.Show("File not found: " + filePath);
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete '{fixtureName}'?",
                "Confirm deletion",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(filePath);
                    System.Windows.MessageBox.Show($"Fixture '{fixtureName}' was deleted.");
                    CurrentViewModel = fixtureListVm;
                    fixtureListVm.ReloadFixturesFromFiles();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error deleting fixture: " + ex.Message);
                }
            }
        }

        // ------------------------------------------------------------
        // IMPORT FIXTURE
        // ------------------------------------------------------------
        private void ImportFixture()
        {
            string downloadsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            var dialog = new OpenFileDialog
            {
                Title = "Select a Fixture JSON file",
                Filter = "JSON files (*.json)|*.json",
                InitialDirectory = downloadsFolder,
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

                // Validate essential fields
                var allowedTypes = new HashSet<string>
                {
                    "Lamp", "Ster", "Klok", "Ventilator", "Rood", "Groen", "Blauw", "Wit",
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
                    string msg = "The following required fields are missing or invalid:\n- " +
                                 string.Join("\n- ", missingFields);
                    System.Windows.MessageBox.Show(msg);
                    return;
                }

                // Use filename (without extension) as fixture name
                string fixtureName = Path.GetFileNameWithoutExtension(jsonPath);

                if (fixtureListVm.Fixtures.Any(f => f.Name.Equals(fixtureName, StringComparison.OrdinalIgnoreCase)))
                {
                    System.Windows.MessageBox.Show($"Fixture '{fixtureName}' already exists.");
                    return;
                }

                // Save fixture into local fixtures folder
                string targetPath = Path.Combine(_fixturesFolder, fixtureName + ".json");
                root["name"] = fixtureName;

                File.WriteAllText(targetPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                fixtureListVm.ReloadFixturesFromFiles();

                System.Windows.MessageBox.Show($"Successfully imported fixture '{fixtureName}'.");
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

        // ------------------------------------------------------------
        // EXPORT FIXTURE
        // ------------------------------------------------------------
        private bool CanExportFixture() => SelectedFixture != null;

        private void ExportFixture()
        {
            if (SelectedFixture == null)
                return;

            string sourcePath = Path.Combine(_fixturesFolder, SelectedFixture.Manufacturer, SelectedFixture.Name + ".json");
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

                // Ask user for new fixture name
                var exportWindow = new ExportFixtureWindow(SelectedFixture.Name)
                {
                    Owner = System.Windows.Application.Current.MainWindow,
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

                    // Save directly to Downloads folder
                    string downloadsFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    Directory.CreateDirectory(downloadsFolder);

                    string exportPath = Path.Combine(downloadsFolder, newName + ".json");
                    File.WriteAllText(exportPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                    System.Windows.MessageBox.Show($"Fixture '{newName}' was exported successfully to Downloads!");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error exporting fixture: " + ex.Message);
            }
        }
    }
}
