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
using Microsoft.Win32;
// ✅ TOEGEVOEGD: Nodig voor debouncing (Task.Delay en CancellationToken)
using System.Threading.Tasks;
using System.Threading;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public partial class FixtureCreateViewModel : ObservableObject
    {
        private readonly string _dataDir;
        private readonly bool _isEditing;
        private readonly string? _originalManufacturer;
        private readonly string? _originalFixtureName;
        private readonly ManufacturerService _manufacturerService;

        // ✅ TOEGEVOEGD: CancellationTokenSource voor het beheren van de debounce-timer
        private CancellationTokenSource? _debounceCts;

        [ObservableProperty]
        private string fixtureName = "New Fixture";

        [ObservableProperty]
        private List<string> _availableManufacturers = new();

        [ObservableProperty]
        private string? _selectedManufacturer;

        // ObservableProperty voor de string-invoer van de TextBox
        [ObservableProperty]
        private string _dmxDivisionsInput = "255";

        // Interne opslag voor de gevalideerde DMX waarde
        private int _dmxDivisions = 255;

        // ObservableProperty om de validatiestatus bij te houden
        [ObservableProperty]
        private bool _isDmxDivisionsValid = true;

        [ObservableProperty]
        private Fixture currentFixture = new Fixture();

        public event EventHandler? BackRequested;
        public event EventHandler? FixtureSaved;
        public ObservableCollection<ChannelViewModel> Channels { get; } = new();

        public ICommand AddChannelCommand { get; }
        public ICommand DeleteChannelCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RegisterManufacturerCommand { get; }
        public ICommand AddImageCommand { get; }

        // Berekent de stapgrootte (TickFrequency)
        public double DmxStepSize
        {
            get
            {
                int safeDivisions = Math.Max(2, Math.Min(255, _dmxDivisions));
                // Vereist: stappen van 255/N
                return 255.0 / safeDivisions;
            }
        }

        // ✅ VERVANGEN: Implementatie met 3 seconden debouncing
        partial void OnDmxDivisionsInputChanged(string value)
        {
            // Annuleer een lopende timer als de gebruiker opnieuw typt
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();

            // Start een nieuwe taak met een vertraging van 3 seconden
            Task.Delay(TimeSpan.FromSeconds(3), _debounceCts.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCanceled) return; // Stop als de taak geannuleerd is

                    // Voer de validatie uit in de UI-thread
                    Application.Current.Dispatcher.Invoke(() => ValidateDmxDivisions(value));

                }, TaskScheduler.Default);
        }

        public FixtureCreateViewModel(FixtureContentViewModel? existing = null)
        {
            // ... (Constructor logica) ...
            _manufacturerService = new ManufacturerService();
            _dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "Fixtures");

            string unknownDir = Path.Combine(_dataDir, "Unknown");
            if (!Directory.Exists(unknownDir))
                Directory.CreateDirectory(unknownDir);

            LoadManufacturers();

            AddChannelCommand = new RelayCommand(AddChannel);
            DeleteChannelCommand = new RelayCommand<ChannelViewModel>(DeleteChannel, CanDeleteChannel);
            SaveCommand = new RelayCommand(SaveFixture, CanSaveFixture);
            CancelCommand = new RelayCommand(Cancel);
            RegisterManufacturerCommand = new RelayCommand(ExecuteRegisterManufacturer);
            AddImageCommand = new RelayCommand(() => AddImage(CurrentFixture));

            if (existing != null)
            {
                _isEditing = true;
                FixtureName = existing.Name ?? string.Empty;
                SelectedManufacturer = existing.Manufacturer ?? "Unknown";
                _originalManufacturer = existing.Manufacturer ?? "Unknown";
                _originalFixtureName = existing.Name ?? string.Empty;

                // Zet de divisies van de bestaande fixture om
                _dmxDivisions = existing.DmxDivisions > 1 ? existing.DmxDivisions : 255;
                DmxDivisionsInput = _dmxDivisions.ToString();

                Channels.Clear();
                foreach (var ch in existing.Channels)
                    Channels.Add(new ChannelViewModel(ch));

                CurrentFixture.Name = FixtureName;
                CurrentFixture.Manufacturer = SelectedManufacturer!;
            }
            else
            {
                _isEditing = false;
                SelectedManufacturer = AvailableManufacturers.FirstOrDefault();
                AddChannel();
            }

            (SaveCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        // ✅ NIEUW: Validatiemethode
        private void ValidateDmxDivisions(string value)
        {
            bool wasValid = IsDmxDivisionsValid;

            if (int.TryParse(value, out int result))
            {
                if (result >= 2 && result <= 255)
                {
                    _dmxDivisions = result;
                    IsDmxDivisionsValid = true;
                    OnPropertyChanged(nameof(DmxStepSize));
                }
                else
                {
                    // Fout: getal buiten bereik (2-255)
                    IsDmxDivisionsValid = false;

                    // Toon de MessageBox alleen als de status net ongeldig is geworden
                    if (wasValid != IsDmxDivisionsValid)
                    {
                        MessageBox.Show(
                            "The DMX divisions must be a number between 2 and 255.",
                            "Input Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                // Fout: geen getal
                IsDmxDivisionsValid = false;

                // Toon de MessageBox alleen als de status net ongeldig is geworden
                if (wasValid != IsDmxDivisionsValid)
                {
                    MessageBox.Show(
                        "The DMX divisions must be a number between 2 and 255.",
                        "Input Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            (SaveCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        private bool CanSaveFixture()
        {
            return IsDmxDivisionsValid;
        }

        private void SaveFixture()
        {
            // ... (Save logica is correct) ...
            if (!IsDmxDivisionsValid)
            {
                MessageBox.Show("De slider divisies zijn ongeldig. Los de fout op voordat u opslaat.", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(FixtureName) || Channels.Any(ch => string.IsNullOrWhiteSpace(ch.Name) || string.IsNullOrEmpty(ch.SelectedType)))
            {
                MessageBox.Show("Vul de volgende velden in (Fixture naam, Kanaal naam, Kanaal type).", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string manufacturer = SelectedManufacturer ?? "Unknown";
            string safeManufacturerName = SanitizeFileName(manufacturer);
            string safeFixtureName = SanitizeFileName(FixtureName);

            string manufacturerDir = Path.Combine(_dataDir, safeManufacturerName);
            if (!Directory.Exists(manufacturerDir))
                Directory.CreateDirectory(manufacturerDir);

            string newFilePath = Path.Combine(manufacturerDir, $"{safeFixtureName}.json");

            if (!_isEditing && File.Exists(newFilePath))
            {
                MessageBox.Show($"Er bestaat al een fixture met de naam: '{FixtureName}' bij '{manufacturer}'. Kies een andere naam.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
                ["dmxDivisions"] = _dmxDivisions,
                ["channels"] = channelsArray,
                ["imagePath"] = CurrentFixture.ImagePath
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = root.ToJsonString(options);

            try
            {
                if (_isEditing && (_originalFixtureName != FixtureName || _originalManufacturer != manufacturer))
                {
                    string safeOriginalManufacturerName = SanitizeFileName(_originalManufacturer!);
                    string safeOriginalFixtureName = SanitizeFileName(_originalFixtureName!);
                    string oldFilePath = Path.Combine(_dataDir, safeOriginalManufacturerName, $"{safeOriginalFixtureName}.json");
                    if (File.Exists(oldFilePath))
                        File.Delete(oldFilePath);
                }

                File.WriteAllText(newFilePath, json);
                MessageBox.Show($"Fixture '{FixtureName}' is succesvol opgeslagen in de map '{manufacturer}'.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadManufacturers();

                FixtureSaved?.Invoke(this, EventArgs.Empty);
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Fout bij het opslaan van de fixture: {ioEx.Message}", "Opslagfout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadManufacturers()
        {
            // ... (LoadManufacturers logica) ...
            var directoryInfo = new DirectoryInfo(_dataDir);
            var currentManufacturers = directoryInfo.GetDirectories()
                                                 .Select(d => d.Name)
                                                 .ToList();
            currentManufacturers.RemoveAll(m => m.Equals("Unknown", StringComparison.OrdinalIgnoreCase));
            var sortedOtherManufacturers = currentManufacturers.OrderBy(m => m).ToList();
            var finalManufacturerList = new List<string> { "Unknown" };
            finalManufacturerList.AddRange(sortedOtherManufacturers);
            AvailableManufacturers = finalManufacturerList;
        }

        private void ExecuteRegisterManufacturer()
        {
            // ... (RegisterManufacturer logica) ...
            var registerWindow = new RegisterManufacturerWindow();
            if (Application.Current.MainWindow != null)
                registerWindow.Owner = Application.Current.MainWindow;

            if (registerWindow.ShowDialog() == true)
            {
                string newManufacturerName = registerWindow.ManufacturerName;
                if (_manufacturerService.RegisterManufacturer(newManufacturerName))
                {
                    string manufacturerDir = Path.Combine(_dataDir, SanitizeFileName(newManufacturerName));
                    if (!Directory.Exists(manufacturerDir))
                        Directory.CreateDirectory(manufacturerDir);

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

        private void AddChannel()
        {
            // ... (AddChannel logica) ...
            var newModel = new Channel
            {
                Name = $"Nieuw Kanaal {Channels.Count + 1}",
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
            var result = MessageBox.Show("Weet je zeker dat je het aanmaken van deze fixture wilt annuleren?", "Bevestigen & verlaten", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
                BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private string SanitizeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            string invalidRegex = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(name, invalidRegex, "_");
        }

        private void AddImage(Fixture fixture)
        {
            // ... (AddImage logica) ...
            string imagesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "Images");

            if (!Directory.Exists(imagesDir))
                Directory.CreateDirectory(imagesDir);

            var dlg = new OpenFileDialog
            {
                Title = "Selecteer een afbeelding",
                Filter = "Afbeeldingen (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Multiselect = false,
                InitialDirectory = imagesDir
            };

            if (dlg.ShowDialog() == true)
            {
                string selectedFile = dlg.FileName;
                string safeFileName = Path.GetFileName(selectedFile);
                string destPath = Path.Combine(imagesDir, safeFileName);

                try
                {
                    if (!File.Exists(destPath))
                    {
                        File.Copy(selectedFile, destPath);
                        MessageBox.Show($"Afbeelding gekopieerd naar:\n{destPath}", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Pad toegevoegd aan fixture.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    fixture.ImagePath = $"Images/{safeFileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kan de afbeelding niet kopiëren:\n{ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}