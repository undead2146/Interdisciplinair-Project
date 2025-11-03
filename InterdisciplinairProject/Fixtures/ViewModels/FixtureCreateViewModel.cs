using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using InterdisciplinairProject.Fixtures.Views;
using InterdisciplinairProject.Fixtures.Services;
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

        // ✅ AANPASSING: Gebruik ObservableCollection voor directe UI updates
        // Hoewel ik het als List<string> houd om zo min mogelijk de bestaande code te verstoren
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

            // Slaat op in [LocalAppData]\InterdisciplinairProject\Fixtures
            _dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "Fixtures");

            // Zorgt ervoor dat de "Unknown" map ALTIJD bestaat bij het opstarten.
            string unknownDir = Path.Combine(_dataDir, "Unknown");
            if (!Directory.Exists(unknownDir))
            {
                Directory.CreateDirectory(unknownDir);
            }


            LoadManufacturers(); // Laadt de fabrikanten

            // Initialiseer commando's
            AddChannelCommand = new RelayCommand(AddChannel);
            DeleteChannelCommand = new RelayCommand<ChannelViewModel>(DeleteChannel, CanDeleteChannel);
            SaveCommand = new RelayCommand(SaveFixture);
            CancelCommand = new RelayCommand(Cancel);
            RegisterManufacturerCommand = new RelayCommand(ExecuteRegisterManufacturer);

            if (existing != null)
            {
                _isEditing = true;
                FixtureName = existing.Name ?? string.Empty;

                SelectedManufacturer = existing.Manufacturer ?? "Unknown";

                _originalManufacturer = existing.Manufacturer ?? "Unknown";
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
                // De geselecteerde fabrikant is de eerste in de lijst, wat nu "Unknown" is.
                SelectedManufacturer = AvailableManufacturers.FirstOrDefault();
                AddChannel();
            }
        }

        // --- FABRIKANT METHODEN ---
        private void LoadManufacturers()
        {
            // ✅ AANPASSING: Wis eerst de bestaande lijst om duplicaten te voorkomen
            // Dit is belangrijk als de Service of een andere methode deze lijst elders beïnvloedt.
            // Dit wordt nu gedaan door de hele lijst opnieuw toe te wijzen na sortering.

            // 1. Leest de namen van fysiek bestaande mappen.
            var directoryInfo = new DirectoryInfo(_dataDir);
            var currentManufacturers = directoryInfo.GetDirectories()
                                                    .Select(d => d.Name)
                                                    .ToList();

            // 2. Verwijder de mapnaam "Unknown" uit de lijst als deze via de mapnamen is gelezen.
            currentManufacturers.RemoveAll(m => m.Equals("Unknown", StringComparison.OrdinalIgnoreCase));

            // 3. Sorteer de overige fabrikanten alfabetisch.
            var sortedOtherManufacturers = currentManufacturers.OrderBy(m => m).ToList();

            // 4. Maak een nieuwe lijst: Begin met "Unknown" en voeg de rest toe.
            var finalManufacturerList = new List<string> { "Unknown" };
            finalManufacturerList.AddRange(sortedOtherManufacturers);

            // 5. Wijs de nieuwe, unieke en gesorteerde lijst toe aan de Observable Property.
            AvailableManufacturers = finalManufacturerList;
        }

        private void ExecuteRegisterManufacturer()
        {
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
                    // Map aanmaken voor de nieuwe fabrikant
                    string manufacturerDir = Path.Combine(_dataDir, SanitizeFileName(newManufacturerName));
                    if (!Directory.Exists(manufacturerDir))
                    {
                        Directory.CreateDirectory(manufacturerDir);
                    }

                    // De dropdown wordt automatisch bijgewerkt NA aanmaken map (via LoadManufacturers).
                    LoadManufacturers();
                    SelectedManufacturer = newManufacturerName;


                    MessageBox.Show($"Manufacturer '{newManufacturerName}' saved succesfully.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
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
            string manufacturer = SelectedManufacturer ?? "Unknown";
            string safeManufacturerName = SanitizeFileName(manufacturer);
            string safeFixtureName = SanitizeFileName(FixtureName);

            // Als de map voor de fabrikant nog niet bestaat, wordt deze aangemaakt
            string manufacturerDir = Path.Combine(_dataDir, safeManufacturerName);
            if (!Directory.Exists(manufacturerDir))
            {
                Directory.CreateDirectory(manufacturerDir);
            }

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
                // OUDE BESTAND VERWIEDEREN (bij hernoemen/verplaatsen)
                if (_isEditing && (_originalFixtureName != FixtureName || _originalManufacturer != manufacturer))
                {
                    string safeOriginalManufacturerName = SanitizeFileName(_originalManufacturer!);
                    string safeOriginalFixtureName = SanitizeFileName(_originalFixtureName!);

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

                // Laad de fabrikanten opnieuw om de lijst bij te werken (indien een nieuwe map is gemaakt).
                LoadManufacturers();

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
            (DeleteChannelCommand as RelayCommand<ChannelViewModel>)?.NotifyCanExecuteChanged();
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
                (DeleteChannelCommand as RelayCommand<ChannelViewModel>)?.NotifyCanExecuteChanged();
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
            // Ongeldige tekens in de fixture- of fabrikantnaam worden verwijderd of vervangen
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            string invalidRegex = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(name, invalidRegex, "_");
        }
    }
}