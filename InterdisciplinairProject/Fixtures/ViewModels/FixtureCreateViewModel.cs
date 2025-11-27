using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Converters;
using InterdisciplinairProject.Fixtures.Models;
using InterdisciplinairProject.Fixtures.Services;
using InterdisciplinairProject.Fixtures.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;


namespace InterdisciplinairProject.Fixtures.ViewModels
{
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

        //partial void OnSliderDivisionsChanged(int value) => OnPropertyChanged(nameof(TickFrequency));

        /// <summary>
        /// ----------------------------------------------------------------------------------------------------------------------------------
        /// </summary>

        [ObservableProperty]
        private string fixtureName = "New Fixture";

        [ObservableProperty]
        private List<string> _availableManufacturers = new();

        [ObservableProperty]
        private string? _selectedManufacturer;

        private FixtureJSON _currentFixture = new FixtureJSON();

        public event EventHandler? FixtureSaved;

        public event EventHandler? BackRequested;

        public ObservableCollection<ChannelItem> Channels { get; } = new();

        public ICommand AddChannelCommand { get; }

        public ICommand DeleteChannelCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand RegisterManufacturerCommand { get; }

        public ICommand AddImageCommand { get; }

        public ICommand AddTypeBtn { get; }

        public FixtureJSON CurrentFixture
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


            string unknownDir = Path.Combine(_dataDir, "Unknown");
            if (!Directory.Exists(unknownDir))
                Directory.CreateDirectory(unknownDir);

            LoadManufacturers();

            AddChannelCommand = new RelayCommand(AddChannel);
            DeleteChannelCommand = new RelayCommand<ChannelItem>(DeleteChannel, CanDeleteChannel);
            SaveCommand = new RelayCommand(SaveFixture);
            CancelCommand = new RelayCommand(Cancel);
            RegisterManufacturerCommand = new RelayCommand(ExecuteRegisterManufacturer);
            AddImageCommand = new RelayCommand<FixtureJSON>(AddImage);

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

                _currentFixture.Name = FixtureName;
                _currentFixture.Manufacturer = SelectedManufacturer!;

                ImageBase64 = ImageCompressionHelpers.DecompressBase64(existing.ImageBase64 ?? string.Empty);
            }
            else
            {
                _isEditing = false;
                SelectedManufacturer = AvailableManufacturers.FirstOrDefault();
                AddChannel();
            }
        }

        private void LoadManufacturers()
        {
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
            var registerWindow = new RegisterManufacturerWindow();
            if (Application.Current.MainWindow != null)
                registerWindow.Owner = Application.Current.MainWindow;

            if (registerWindow.ShowDialog() == true)
            {
                string newManufacturerName = registerWindow.ManufacturerName;
                if (_manufacturerService.RegisterManufacturer(newManufacturerName))
                {
                    LoadManufacturers();
                    SelectedManufacturer = newManufacturerName;
                    SaveManufacturersToJson();

                    MessageBox.Show($"Manufacturer '{newManufacturerName}' saved succesfully.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Manufacturer '{newManufacturerName}' can't be saved. Name is empty or there already exists a manufacturer with the same name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveManufacturersToJson() 
        {
            try
            {
                string jsonPath = Path.Combine(_dataDir, "manufacturers.json");

                Directory.CreateDirectory(_dataDir);

                var json = JsonSerializer.Serialize(AvailableManufacturers, new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

                File.WriteAllText(jsonPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save manufacturers JSON:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveFixture()
        {
            if (string.IsNullOrEmpty(FixtureName) || Channels.Any(ch => string.IsNullOrWhiteSpace(ch.Name) || string.IsNullOrEmpty(ch.SelectedType)))
            {
                MessageBox.Show("Please fill in the following (Name Fixture, Name channel, Channel type).", "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"There already exist a fixture with name: '{FixtureName}' assigned to '{manufacturer}'. Please choose another.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var channelsArray = new JsonArray();
            foreach (var ch in Channels)
            {
                var channelObj = new JsonObject
                {
                    ["Name"] = ch.Name,
                    ["Type"] = ch.SelectedType,
                    ["value"] = ch.Level.ToString(),
                };
                channelsArray.Add(channelObj);
            }

            // json root
            var root = new JsonObject
            {
                ["name"] = FixtureName,
                ["manufacturer"] = manufacturer,
                ["channels"] = channelsArray,
                ["imageBase64"] = !string.IsNullOrEmpty(ImageBase64) ? ImageCompressionHelpers.CompressBase64(ImageBase64) : _currentFixture.ImageBase64,
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
                MessageBox.Show($"Fixture '{FixtureName}' is succesfully saved in '{manufacturer}' map.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadManufacturers();

                FixtureSaved?.Invoke(this, EventArgs.Empty);

                if (!_isEditing) {  //only go back if you were editing
                BackRequested?.Invoke(this, EventArgs.Empty); 
                }
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Error with saving fixture: {ioEx.Message}", "Save error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddChannel()
        {
            var newModel = new Channel
            {
                Name = "Lamp",
                Type = "Lamp",
                Value = "0"
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

        private void AddImage(FixtureJSON fixture)
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"unable to Load image:\n{ex.Message}", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string SanitizeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            string invalidRegex = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(name, invalidRegex, "_");
        }
    }
}
