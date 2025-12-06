using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Models;  
using InterdisciplinairProject.Core.Models.Enums;
using InterdisciplinairProject.Fixtures.Services;
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
        // ** NIEUW: Field voor de Manufacturer ViewModel **
        private ManufacturerViewModel? _manufacturerViewModel;

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
        // ** NIEUW: Command voor de Fabrikanten knop **
        public ICommand ShowManufacturerCommand { get; }

        public ICommand GoBackCommand { get; }

        public MainWindowFixturesViewModel()
        {
            CreateFixtureCommand = new RelayCommand(CreateFixture);
            DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this, EventArgs.Empty));
            ImportFixtureCommand = new RelayCommand(ImportFixture);
            ExportFixtureCommand = new RelayCommand(ExportFixture, CanExportFixture);
            // ** NIEUW: Initialiseer het Fabrikanten Command **
            ShowManufacturerCommand = new RelayCommand(ShowManufacturer);
            GoBackCommand = new RelayCommand(ExecuteGoBack);

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

        // ------------------------------------------------------------
        // ✨ NAVIGATIE METHODE VOOR DE 'BACK' KNOP
        // ------------------------------------------------------------
        private void ExecuteGoBack()
        {
         

            
            CurrentViewModel = fixtureListVm;
        }



        // ** NIEUW: Methode om de Manufacturer View te tonen **
        private void ShowManufacturer()
        {
            if (_manufacturerViewModel == null)
            {
                _manufacturerViewModel = new ManufacturerViewModel();
                // Abonneer op het update event zodat de hoofdlijst met fixtures herlaadt als een fabrikant wordt hernoemd of verwijderd.
                _manufacturerViewModel.ManufacturersUpdated += OnManufacturersUpdated;
            }
            CurrentViewModel = _manufacturerViewModel;
        }

        // ** NIEUW: Event Handler om de fixture lijst te herladen **
        private void OnManufacturersUpdated(object? sender, EventArgs e)
        {
            // Zorgt ervoor dat de fabrikantenlijst in de FixtureListViewModel wordt bijgewerkt
            // en dus de dropdowns in de Create/Edit view ook.
            fixtureListVm.ReloadFixturesFromFiles();
        }
        private void OnFixtureSelected(object? sender, string json)
        {
            var detailVm = new FixtureContentViewModel(json);
            detailVm.BackRequested += (_, __) => CurrentViewModel = fixtureListVm;
            detailVm.DeleteRequested += (_, __) => OnFixtureDelete(detailVm.Name, detailVm.Manufacturer);
            detailVm.EditRequested += (_, contentVm) =>
            {
                var editVm = new FixtureCreateViewModel(contentVm);
                //editVm.BackRequested += (_, __) => CurrentViewModel = fixtureListVm;
                //editVm.FixtureSaved += (_, __) =>
                //{
                //    fixtureListVm.ReloadFixturesFromFiles();
                //    CurrentViewModel = fixtureListVm;
                //};

                // CANCEL while editing → back to content view of original fixture
                editVm.BackRequested += (_, __) =>
                {
                    try
                    {
                        // use the ORIGINAL name/manufacturer from the content VM (before editing)
                        string manufacturer = contentVm.Manufacturer ?? "Unknown";
                        string name = contentVm.Name ?? string.Empty;
                        string filePath = Path.Combine(_fixturesFolder, manufacturer, name + ".json");

                        if (File.Exists(filePath))
                        {
                            string json2 = File.ReadAllText(filePath);
                            OnFixtureSelected(this, json2);   // reopen FixtureContentView
                        }
                        else
                        {
                            // if file somehow missing, fall back to list
                            CurrentViewModel = fixtureListVm;
                        }
                    }
                    catch
                    {
                        CurrentViewModel = fixtureListVm;
                    }
                };

                editVm.FixtureSaved += (_, __) =>
                {
                    fixtureListVm.ReloadFixturesFromFiles();

                    try
                    {
                        // use the (possibly changed) name/manufacturer from the editor
                        string manufacturer = editVm.SelectedManufacturer ?? "Unknown";
                        string name = editVm.FixtureName;
                        string filePath = Path.Combine(_fixturesFolder, manufacturer, name + ".json");

                        if (File.Exists(filePath))
                        {
                            string json2 = File.ReadAllText(filePath);
                            OnFixtureSelected(this, json2);   // <-- go back to FixtureContentView
                        }
                        else
                        {
                            // fallback if something went wrong
                            CurrentViewModel = fixtureListVm;
                        }
                    }
                    catch
                    {
                        CurrentViewModel = fixtureListVm;
                    }
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
            string filePath = Path.Combine(_fixturesFolder, manufacturerName, fixtureName + ".json");

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

                Fixture fixture;

                if (root["availableChannels"] != null) // OFL JSON
                {
                    fixture = new Fixture
                    {
                        FixtureId = "",
                        InstanceId = "",
                        Name = root["name"]?.ToString() ?? "Unnamed",
                        Manufacturer = root["manufacturerKey"]?.ToString() ?? "Unknown",
                        Description = "",
                        StartAddress = 1,
                        ImageBase64 = ""
                    };

                    fixture.Channels.Clear();

                    var channelsNode = root["availableChannels"] as JsonObject;
                    if (channelsNode != null)
                    {
                        foreach (var kvp in channelsNode)
                        {
                            var ch = new Channel
                            {
                                Name = kvp.Key,
                                Type = kvp.Key,
                                Value = "0",
                                Min = 0,
                                Max = 255,
                                Time = 0,
                                ChannelEffect = new ChannelEffect
                                {
                                    Enabled = false,
                                    EffectType = EffectType.Custom,
                                    Time = 0,
                                    Min = 0,
                                    Max = 255,
                                    Parameters = new Dictionary<string, object>()
                                }
                            };
                            fixture.Channels.Add(ch);

                            // register channel type
                            TypeCatalogService.AddOrUpdate(new TypeSpecification
                            {
                                name = ch.Type,
                                input = "slider",
                                min = 0,
                                max = 255
                            });
                        }
                    }
                }
                else // native JSON
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };
                    fixture = JsonSerializer.Deserialize<Fixture>(jsonContent, options)
                              ?? throw new JsonException("Invalid fixture JSON.");

                    ApplyDefaults(fixture);

                    // register channel types
                    foreach (var ch in fixture.Channels)
                    {
                        TypeCatalogService.AddOrUpdate(new TypeSpecification
                        {
                            name = ch.Type,
                            input = "slider",
                            min = 0,
                            max = 255
                        });
                    }
                }

                string fixtureName = Path.GetFileNameWithoutExtension(jsonPath);
                fixture.Name = fixtureName;

                if (string.IsNullOrWhiteSpace(fixture.Manufacturer))
                    fixture.Manufacturer = "Unknown";

                // register manufacturer
                var manufacturerService = new ManufacturerService();
                manufacturerService.RegisterManufacturer(fixture.Manufacturer);

                bool exists = fixtureListVm.ManufacturerGroups
                    .SelectMany(g => g.Fixtures)
                    .Any(f => f.Name.Equals(fixtureName, StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    System.Windows.MessageBox.Show($"Fixture '{fixtureName}' already exists.");
                    return;
                }

                string manufacturerFolder = Path.Combine(_fixturesFolder, fixture.Manufacturer);
                Directory.CreateDirectory(manufacturerFolder);
                string targetPath = Path.Combine(manufacturerFolder, fixtureName + ".json");

                string outputJson = JsonSerializer.Serialize(fixture, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(targetPath, outputJson);

                fixtureListVm.ReloadFixturesFromFiles();

                System.Windows.MessageBox.Show($"Successfully imported fixture '{fixtureName}'.");
            }
            catch (JsonException ex)
            {
                System.Windows.MessageBox.Show(
                    $"Invalid JSON format:\n\n{ex.Message}\n\n" +
                    $"Path: {ex.Path}\n" +
                    $"Line: {ex.LineNumber}, Byte: {ex.BytePositionInLine}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error importing fixture: " + ex.Message);
            }
        }

        private void ApplyDefaults(Fixture f)
        {
            f.FixtureId ??= "";
            f.InstanceId ??= "";
            f.Name ??= "Unnamed";
            f.Manufacturer ??= "Unknown";
            f.Description ??= "";
            f.ImageBase64 ??= "";
            if (f.StartAddress <= 0) f.StartAddress = 1;

            foreach (var ch in f.Channels)
            {
                ch.Name ??= "";
                ch.Type ??= "";
                ch.Value ??= "0";

                if (ch.Min < 0) ch.Min = 0;
                if (ch.Max == 0) ch.Max = 255;
                if (ch.Time < 0) ch.Time = 0;

                ch.Ranges ??= new List<ChannelRange>();

                if (ch.ChannelEffect == null)
                    ch.ChannelEffect = new ChannelEffect();
                else if (ch.ChannelEffect.Parameters == null)
                    ch.ChannelEffect.Parameters = new Dictionary<string, object>();
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
