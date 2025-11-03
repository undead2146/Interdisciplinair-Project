using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using InterdisciplinairProject.Fixtures.Views; // Voor RegisterManufacturerWindow
using InterdisciplinairProject.Fixtures.Services; // Voor ManufacturerService
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using System;
using System.Text.RegularExpressions;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public partial class FixtureCreateViewModel : ObservableObject
    {
        // --- VELDEN ---
        private readonly string _dataDir;
        private readonly bool _isEditing;
        private readonly string? _originalManufacturer;
        private readonly string? _originalFixtureName;
        private readonly ManufacturerService _manufacturerService;

        // --- Observable Properties ---
        [ObservableProperty]
        private string fixtureName = "New Fixture";

        [ObservableProperty]
        private List<string> _availableManufacturers = new();

        [ObservableProperty]
        private string? _selectedManufacturer;

        // --- EVENTS & COLLECTIONS ---
        public event EventHandler? BackRequested;
        public event EventHandler? FixtureSaved;
        public ObservableCollection<ChannelViewModel> Channels { get; } = new();

        // --- COMMANDO'S ---
        public ICommand AddChannelCommand { get; }
        public ICommand DeleteChannelCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RegisterManufacturerCommand { get; }



        // --- CONSTRUCTOR ---
        public FixtureCreateViewModel(FixtureContentViewModel? existing = null)
        {
            _manufacturerService = new ManufacturerService();

            // 💡 CRUCIALE AANPASSING VOOR PAD:
            // Slaat op in [Uitvoeringsmap]\data
            _dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "Fixtures");


            LoadManufacturers(); // Laadt de fabrikanten

            // Initialiseer commando's in de constructor (Oplossing CS0236)
            AddChannelCommand = new RelayCommand(AddChannel);
            DeleteChannelCommand = new RelayCommand<ChannelViewModel>(DeleteChannel, CanDeleteChannel);
            SaveCommand = new RelayCommand(SaveFixture);
            CancelCommand = new RelayCommand(Cancel);
            RegisterManufacturerCommand = new RelayCommand(ExecuteRegisterManufacturer);

            if (existing != null)
            {
                _isEditing = true;
                FixtureName = existing.Name ?? string.Empty;

                SelectedManufacturer = existing.Manufacturer ?? "None";

                _originalManufacturer = existing.Manufacturer ?? "None";
                _originalFixtureName = existing.Name ?? string.Empty;

                Channels.Clear();
                foreach (var ch in existing.Channels)
                {
                    Channels.Add(new ChannelViewModel(ch));
                }
            }
            else
            {
                _isEditing = false;
                SelectedManufacturer = AvailableManufacturers.FirstOrDefault() ?? "None";
                AddChannel();
            }
        }

        // --- FABRIKANT METHODEN ---
        private void LoadManufacturers()
        {
            // De dropdown toont alle geregistreerde fabrikanten
            AvailableManufacturers = _manufacturerService.GetManufacturers();

            // 💡 AANPASSING 1/2: "Default" vervangen door "Unknown"
            if (!AvailableManufacturers.Any(m => m.Equals("None", StringComparison.OrdinalIgnoreCase)))
            {
                AvailableManufacturers.Insert(0, "Unknown");
            }
        }

        private void ExecuteRegisterManufacturer()
        {
            // US: Er is een knop "Nieuwe fabrikant registreren" aanwezig
            var registerWindow = new RegisterManufacturerWindow();
            if (Application.Current.MainWindow != null)
            {
                registerWindow.Owner = Application.Current.MainWindow;
            }

            if (registerWindow.ShowDialog() == true)
            {
                string newManufacturerName = registerWindow.ManufacturerName;
                if (_manufacturerService.RegisterManufacturer(newManufacturerName))
                {
                    // US: De dropdown wordt automatisch bijgewerkt
                    LoadManufacturers();
                    SelectedManufacturer = newManufacturerName;

                    // US: Als een nieuwe fabrikant wordt aangemaakt -> folder voor deze fabrikant wordt aangemaakt
                    // De map wordt direct onder de _dataDir (data) gemaakt.
                    string manufacturerDir = Path.Combine(_dataDir, SanitizeFileName(newManufacturerName));
                    if (!Directory.Exists(manufacturerDir))
                    {
                        Directory.CreateDirectory(manufacturerDir);
                    }

                    MessageBox.Show($"Manufacturer '{newManufacturerName}' saved succesfully.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // US: Fabrikantnaam mag niet leeg zijn en mag niet al bestaan
                    MessageBox.Show($"Manufacturer '{newManufacturerName}' can't be saved. Name is empty or there already exists a manufacturer with the same name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- OPSLAAN METHODE ---
        private void SaveFixture()
        {
            // --- VALIDATIE ---
            if (string.IsNullOrEmpty(FixtureName) || Channels.Any(ch => string.IsNullOrWhiteSpace(ch.Name) || string.IsNullOrEmpty(ch.SelectedType)))
            {
                MessageBox.Show("Please fill in the following (Name Fixture, Name channel, Channel type).", "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // --- PAD PREPARATIE ---
            // 💡 AANPASSING 2/2: Standaardwaarde voor fabrikant bij opslaan is nu "Unknown"
            string manufacturer = SelectedManufacturer ?? "Unknown";
            string safeManufacturerName = SanitizeFileName(manufacturer);
            string safeFixtureName = SanitizeFileName(FixtureName);

            // US: Als de map voor de fabrikant nog niet bestaat, wordt deze automatisch aangemaakt
            // manufacturerDir is nu [ProjectDir]\data\[fabrikantnaam]
            string manufacturerDir = Path.Combine(_dataDir, safeManufacturerName);
            if (!Directory.Exists(manufacturerDir))
            {
                Directory.CreateDirectory(manufacturerDir);
            }

            // US: Bij het opslaan van een fixture wordt het bestand opgeslagen in data/fabrikant.
            string newFilePath = Path.Combine(manufacturerDir, $"{safeFixtureName}.json");

            // Dubbele Bestandsnaam Check
            if (!_isEditing && File.Exists(newFilePath))
            {
                MessageBox.Show($"There already exist a fixture with name: '{FixtureName}' assigned to '{manufacturer}'. Please choose another.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // --- JSON CREATIE ---
            var channelsArray = new JsonArray();
            foreach (var ch in Channels)
            {
                var channelObj = new JsonObject
                {
                    ["Name"] = ch.Name,
                    ["Type"] = ch.SelectedType,
                    ["value"] = ch.Parameter,
                };
                channelsArray.Add(channelObj);
            }

            var root = new JsonObject
            {
                ["name"] = FixtureName,
                ["manufacturer"] = manufacturer,
                ["channels"] = channelsArray,
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = root.ToJsonString(options);

            // --- OPSLAG ---
            try
            {

                // OUDE BESTAND VERWIJDEREN (bij hernoemen/verplaatsen)
                if (_isEditing && (_originalFixtureName != FixtureName || _originalManufacturer != manufacturer))
                {
                    string safeOriginalManufacturerName = SanitizeFileName(_originalManufacturer!);
                    string safeOriginalFixtureName = SanitizeFileName(_originalFixtureName!);

                    // Old path is nu ook aangepast naar [ProjectDir]\data/[fabrikantnaam]/[fixturenaam].json
                    string oldFilePath = Path.Combine(
                        _dataDir,
                        safeOriginalManufacturerName,
                        $"{safeOriginalFixtureName}.json");

                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }

                // NIEUW BESTAND OPSLAAN
                File.WriteAllText(newFilePath, json);
                MessageBox.Show($"Fixture '{FixtureName}' is succesfully saved in '{manufacturer}' map.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                FixtureSaved?.Invoke(this, EventArgs.Empty);
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Error with saving fixture: {ioEx.Message}", "Save error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- HULP METHODEN ---
        private void AddChannel()
        {
            var newModel = new Channel
            {
                Name = $"New Channel {Channels.Count + 1}",
                Type = "Lamp",
                Value = "0"
            };
            Channels.Add(new ChannelViewModel(newModel));
            (DeleteChannelCommand as RelayCommand<ChannelViewModel>)?.NotifyCanExecuteChanged(); //allows first channel to be deleted after addition or deletetion
        }

        private bool CanDeleteChannel(ChannelViewModel? channel)
        {
            return channel != null && Channels.Count > 1;
        }

        private void DeleteChannel(ChannelViewModel? channel)
        {
            if (channel != null)
            {
                Channels.Remove(channel);
                (DeleteChannelCommand as RelayCommand<ChannelViewModel>)?.NotifyCanExecuteChanged(); //allows first channel to be deleted after addition or deletetion
            }
        }

        private void Cancel()
        {
            var result = MessageBox.Show(
             messageBoxText: "Are you sure that you want to cancel making this fixture?",
             caption: "Confirm & exit",
             button: MessageBoxButton.YesNo,
             icon: MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private string SanitizeFileName(string name)
        {
            // US: Ongeldige tekens in de fixture- of fabrikantnaam worden verwijderd of vervangen
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            string invalidRegex = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(name, invalidRegex, "_");
        }
    }
}