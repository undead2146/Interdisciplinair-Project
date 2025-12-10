using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Fixtures.Converters;
using InterdisciplinairProject.Fixtures.Services;
using InterdisciplinairProject.Fixtures.Views;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    /// <summary>
    /// ViewModel for creating or editing fixtures.
    /// </summary>
    public partial class FixtureCreateViewModel : ObservableObject
    {
        private readonly string _dataDir = Path.Combine(
                                           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                           "InterdisciplinairProject",
                                           "Fixtures");
        private readonly bool _isEditing;
        private readonly string? _originalManufacturer;
        private readonly string? _originalFixtureName;
        private readonly ManufacturerService _manufacturerService;

        [ObservableProperty]
        private ChannelItem? selectedChannel;

        // partial void OnSliderDivisionsChanged(int value) => OnPropertyChanged(nameof(TickFrequency));
        [ObservableProperty]
        private string fixtureName = "New Fixture";

        [ObservableProperty]
        private List<string> _availableManufacturers = new();

        [ObservableProperty]
        private string? _selectedManufacturer;

        [ObservableProperty]
        private string? newManufacturerName;

        private Fixture _currentFixture = new Fixture();

        /// <summary>
        /// Occurs when a fixture is saved.
        /// </summary>
        public event EventHandler? FixtureSaved;

        /// <summary>
        /// Occurs when the user requests to go back.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Gets the collection of channels.
        /// </summary>
        public ObservableCollection<ChannelItem> Channels { get; } = new();


        /// <summary>
        /// Gets the command to add a channel.
        /// </summary>
        public ICommand AddChannelCommand { get; }

        /// <summary>
        /// Gets the command to delete a channel.
        /// </summary>
        public ICommand DeleteChannelCommand { get; }

        /// <summary>
        /// Gets the command to save the fixture.
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Gets the command to cancel.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Gets the command to register a manufacturer.
        /// </summary>
        public ICommand RegisterManufacturerCommand { get; }

        /// <summary>
        /// Gets the command to add an image.
        /// </summary>
        public ICommand AddImageCommand { get; }

        public ICommand AddTypeBtn { get; }

        public Fixture CurrentFixture
        {
            get => _currentFixture;
            set
            {
                _currentFixture = value;
                OnPropertyChanged(nameof(CurrentFixture));
                OnPropertyChanged(nameof(ImageBase64));
            }
        }

        private string _imageBase64 = string.Empty;

        /// <summary>
        /// Gets or sets the base64 encoded image.
        /// </summary>
        public string ImageBase64
        {
            get => _imageBase64;
            set
            {
                if (_imageBase64 != value)
                {
                    _imageBase64 = value;
                    OnPropertyChanged(); // als je INotifyPropertyChanged gebruikt
                }
            }
        }

        public FixtureCreateViewModel(FixtureContentViewModel? existing = null)
        {
            _manufacturerService = new ManufacturerService();
            LoadManufacturers();
            RegisterManufacturerCommand = new RelayCommand(ExecuteRegisterManufacturer);

            string unknownDir = Path.Combine(_dataDir, "Unknown");
            if (!Directory.Exists(unknownDir))
                Directory.CreateDirectory(unknownDir);

            AddChannelCommand = new RelayCommand(AddChannel);
            DeleteChannelCommand = new RelayCommand<ChannelItem>(DeleteChannel, CanDeleteChannel);
            SaveCommand = new RelayCommand(SaveFixture);
            CancelCommand = new RelayCommand(Cancel);
            RegisterManufacturerCommand = new RelayCommand(ExecuteRegisterManufacturer);

            AddImageCommand = new RelayCommand(AddImage);

            if (existing != null)
            {
                _isEditing = true;
                FixtureName = existing.Name ?? string.Empty;
                SelectedManufacturer = existing.Manufacturer ?? "Unknown";
                _originalManufacturer = existing.Manufacturer ?? "Unknown";
                _originalFixtureName = existing.Name ?? string.Empty;

                Channels.Clear();
                foreach (var ch in existing.Channels)
                    Channels.Add(new ChannelItem(ch));

                _currentFixture = new Fixture
                {
                    Name = FixtureName,
                    Manufacturer = SelectedManufacturer!,
                    Channels = new ObservableCollection<Channel>(existing.Channels),
                    ImageBase64 = ImageCompressionHelpers.DecompressBase64(existing.ImageBase64 ?? string.Empty)
                };
                ImageBase64 = _currentFixture.ImageBase64;
            }
            else
            {
                _isEditing = false;
                SelectedManufacturer = AvailableManufacturers.FirstOrDefault();
                _currentFixture = new Fixture(); // 🔹 altijd een geldige Fixture
                AddChannel();
            }
        }

        /*********** MANUFACTURERS SAVEN & LOADEN ***********/
        private void LoadManufacturers()
        {
            AvailableManufacturers = _manufacturerService.LoadManufacturersFromJson();

            if (AvailableManufacturers.Count == 0)
                AvailableManufacturers = _manufacturerService.GetManufacturers();

            if (!AvailableManufacturers.Contains("Unknown"))
                AvailableManufacturers.Insert(0, "Unknown");
        }

        private void ExecuteRegisterManufacturer()
        {
            string name = NewManufacturerName?.Trim() ?? string.Empty;

            if (_manufacturerService.RegisterManufacturer(name))
            {
                LoadManufacturers();
                SelectedManufacturer = name;

                MessageBox.Show($"Manufacturer '{name}' saved succesfully.",
                                "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                NewManufacturerName = string.Empty;
            }
            else
            {
                MessageBox.Show($"Manufacturer '{name}' can't be saved. Name is empty or duplicate.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /*********** FIXTURES SAVEN ***********/
        private string SanitizeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            string invalidRegex = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(name, invalidRegex, "_");
        }

        private void SaveFixture()
        {
            // 🔎 Validatie
            if (Channels.Any(ch =>
                    string.Equals(ch.SelectedType, "Select a type", StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("A channel hasn't been assigned a type.",
                                "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(FixtureName) ||
                Channels.Any(ch => string.IsNullOrWhiteSpace(ch.Name) || string.IsNullOrEmpty(ch.SelectedType)))
            {
                MessageBox.Show("Please fill in the following (Name Fixture, Name channel, Channel type).",
                    "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check for duplicate channel names
            var duplicateChannelNames = Channels
                .GroupBy(ch => ch.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateChannelNames.Any())
            {
                string duplicates = string.Join(", ", duplicateChannelNames);
                MessageBox.Show($"Channel names must be unique. Duplicate names found: {duplicates}",
                    "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 🔧 Vul het Fixture model
            _currentFixture.Name = FixtureName;
            _currentFixture.Manufacturer = SelectedManufacturer ?? "Unknown";
            _currentFixture.ImageBase64 = !string.IsNullOrEmpty(ImageBase64)
                ? ImageCompressionHelpers.CompressBase64(ImageBase64)
                : string.Empty;
            _currentFixture.Channels = new ObservableCollection<Channel>(
                Channels.Select(ci => ci.ToModel())
            );

            // 🔧 Bestandsnaam en map
            string manufacturer = _currentFixture.Manufacturer;
            string safeManufacturerName = SanitizeFileName(manufacturer);
            string safeFixtureName = SanitizeFileName(_currentFixture.Name);

            string manufacturerDir = Path.Combine(_dataDir, safeManufacturerName);
            if (!Directory.Exists(manufacturerDir))
                Directory.CreateDirectory(manufacturerDir);

            string newFilePath = Path.Combine(manufacturerDir, $"{safeFixtureName}.json");

            if (!_isEditing && File.Exists(newFilePath))
            {
                MessageBox.Show($"There already exists a fixture with name: '{FixtureName}' assigned to '{manufacturer}'. Please choose another.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 🔧 Serialiseer met camelCase + enum converter
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            string json = JsonSerializer.Serialize(_currentFixture, options);

            try
            {
                // 🔧 Verwijder oude file bij rename
                if (_isEditing && (_originalFixtureName != FixtureName || _originalManufacturer != manufacturer))
                {
                    string safeOriginalManufacturerName = SanitizeFileName(_originalManufacturer!);
                    string safeOriginalFixtureName = SanitizeFileName(_originalFixtureName!);
                    string oldFilePath = Path.Combine(_dataDir, safeOriginalManufacturerName, $"{safeOriginalFixtureName}.json");
                    if (File.Exists(oldFilePath))
                        File.Delete(oldFilePath);
                }

                File.WriteAllText(newFilePath, json);
                MessageBox.Show($"Fixture '{FixtureName}' is succesfully saved in '{manufacturer}' folder.",
                    "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadManufacturers();
                FixtureSaved?.Invoke(this, EventArgs.Empty);

                if (!_isEditing)
                    BackRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Error saving fixture: {ioEx.Message}", "Save error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddChannel()
        {
            if (Channels.Count >= 512)
            {
                MessageBox.Show("Maximum of 512 channels reached.", "Limit reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Generate unique channel name by counting existing channels
            int channelNumber = Channels.Count + 1;
            string channelName = $"Channel {channelNumber}";

            // Ensure uniqueness in case user deleted middle channels
            while (Channels.Any(c => c.Name == channelName))
            {
                channelNumber++;
                channelName = $"Channel {channelNumber}";
            }

            var newModel = new Channel
            {
                Name = channelName,
                Type = "Select a type",
                Value = "0",
                Min = 0,
                Max = 255,
            };
            Channels.Add(new ChannelItem(newModel));
            (DeleteChannelCommand as RelayCommand<ChannelItem>)?.NotifyCanExecuteChanged();
        }

        private bool CanDeleteChannel(ChannelItem? channel)
        {
            return channel != null && Channels.Count > 1;
        }

        private void DeleteChannel(ChannelItem? channel)
        {
            if (channel != null)
            {
                Channels.Remove(channel);
                (DeleteChannelCommand as RelayCommand<ChannelItem>)?.NotifyCanExecuteChanged();
            }
        }

        private void Cancel()
        {
            var result = MessageBox.Show("Are you sure that you want to cancel making this fixture?", "Confirm & exit", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
                BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void AddImage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select an image",
                Filter = "images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Multiselect = false,
            };

            if (dlg.ShowDialog() == true)
            {
                string selectedFile = dlg.FileName;
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(selectedFile);
                    ImageBase64 = Convert.ToBase64String(imageBytes);
                    _currentFixture.ImageBase64 = ImageBase64;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to load image:\n{ex.Message}", "Failed",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
