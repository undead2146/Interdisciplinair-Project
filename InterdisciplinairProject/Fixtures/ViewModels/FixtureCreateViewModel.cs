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

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public partial class FixtureCreateViewModel : ObservableObject
    {
        private readonly string _dataDir;
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

        public event EventHandler? BackRequested;

        public event EventHandler? FixtureSaved;

        public ObservableCollection<ChannelItem> Channels { get; } = new();

        public ICommand AddChannelCommand { get; }

        public ICommand DeleteChannelCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand RegisterManufacturerCommand { get; }

        public ICommand AddImageCommand { get; }

        public ICommand AddTypeBtn { get; }

        private Fixture _currentFixture = new Fixture();

        public FixtureCreateViewModel(FixtureContentViewModel? existing = null)
        {
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
            DeleteChannelCommand = new RelayCommand<ChannelItem>(DeleteChannel, CanDeleteChannel);
            SaveCommand = new RelayCommand(SaveFixture);
            CancelCommand = new RelayCommand(Cancel);
            RegisterManufacturerCommand = new RelayCommand(ExecuteRegisterManufacturer);
            AddImageCommand = new RelayCommand<Fixture>(AddImage);

            

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

            var root = new JsonObject
            {
                ["name"] = FixtureName,
                ["manufacturer"] = manufacturer,
                ["channels"] = channelsArray,
                ["imagePath"] = _currentFixture.ImagePath
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
                BackRequested?.Invoke(this, EventArgs.Empty);
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
                Name = $"New Channel {Channels.Count + 1}",
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

        private string SanitizeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            string invalidRegex = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(name, invalidRegex, "_");
        }

        private void AddImage(Fixture fixture)
        {
            string imagesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "Images");

            if (!Directory.Exists(imagesDir))
                Directory.CreateDirectory(imagesDir);

            var dlg = new OpenFileDialog
            {
                Title = "Select an image",
                Filter = "images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
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
                        MessageBox.Show($"image copied to:\n{destPath}", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"path added to fixture.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    _currentFixture.ImagePath = $"Images/{safeFileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"unable to copy image:\n{ex.Message}", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public partial class ChannelItem : ObservableObject
        {
            private readonly Channel _model;

            [ObservableProperty]
            private bool isEditing;

            [ObservableProperty]
            private bool isExpanded;

            public ChannelItem(Channel model)
            {
                _model = model;

                Name = _model.Name;
                SelectedType = string.IsNullOrWhiteSpace(_model.Type) ? "Lamp" : _model.Type!;
                if (int.TryParse(_model.Value, out var lvl)) Level = lvl;

                TypeCatalogService.EnsureLoaded();
                ApplyTypeSpec(SelectedType);

                AddCustomTypeCommand = new RelayCommand(DoAddCustomType);
            }

            // Name <-> model
            private string _name = "";
            public string Name
            {
                get => _name;
                set { if (SetProperty(ref _name, value)) _model.Name = value; }
            }

            // SelectedType drives panels
            private string _selectedType = "Lamp";
            public string SelectedType
            {
                get => _selectedType;
                set
                {
                    if (SetProperty(ref _selectedType, value))
                    {
                        _model.Type = value;
                        ApplyTypeSpec(value);
                        if (IsSliderType) Level = Level; // re-snap
                    }
                }
            }

            // Slider level <-> model.Value
            private int _level;
            public int Level
            {
                get => _level;
                set
                {
                    var snapped = Snap(value, Math.Max(1, SliderDivisions));
                    if (SetProperty(ref _level, snapped))
                        _model.Value = snapped.ToString();
                }
            }

            public IReadOnlyList<string> AvailableTypes => TypeCatalogService.Names;

            private bool _isSliderType;
            public bool IsSliderType
            {
                get => _isSliderType;
                set => SetProperty(ref _isSliderType, value);
            }

            private bool _isCustomType;
            public bool IsCustomType
            {
                get => _isCustomType;
                set => SetProperty(ref _isCustomType, value);
            }

            private int _sliderDivisions = 255;
            public int SliderDivisions
            {
                get => _sliderDivisions;
                set
                {
                    if (SetProperty(ref _sliderDivisions, value))
                        OnPropertyChanged(nameof(TickFrequency));
                }
            }

            public int TickFrequency => Math.Max(1, 255 / Math.Max(1, SliderDivisions));

            // Custom panel fields
            private string? _customTypeName;
            public string? CustomTypeName
            {
                get => _customTypeName;
                set => SetProperty(ref _customTypeName, value);
            }

            private int _customTypeSliderValue;
            public int CustomTypeSliderValue
            {
                get => _customTypeSliderValue;
                set => SetProperty(ref _customTypeSliderValue, value);
            }

            public IRelayCommand AddCustomTypeCommand { get; }

            private void DoAddCustomType()
            {
                var name = (CustomTypeName ?? "").Trim();
                var divisions = CustomTypeSliderValue;

                if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Type name is empty."); return; }
                if (divisions <= 0 || divisions > 255) { MessageBox.Show("Step value must be between 1 and 255."); return; }
                if (string.Equals(name, "Custom", StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("Choose another name than 'Custom'."); return; }

                var spec = new TypeSpecification { name = name, input = "slider", divisions = divisions };
                if (!TypeCatalogService.AddOrUpdate(spec)) { MessageBox.Show("Failed to save the type."); return; }

                OnPropertyChanged(nameof(AvailableTypes)); // refresh ComboBox
                SelectedType = name;                       // becomes slider with divisions
                IsCustomType = false;                      // hide custom panel
            }

            private void ApplyTypeSpec(string? typeName)
            {
                IsSliderType = false;
                IsCustomType = false;

                var spec = TypeCatalogService.GetByName(typeName);
                if (spec == null) return;

                if (spec.input.Equals("slider", StringComparison.OrdinalIgnoreCase))
                {
                    IsSliderType = true;
                    SliderDivisions = spec.divisions.GetValueOrDefault(255);
                }
                else if (spec.input.Equals("custom", StringComparison.OrdinalIgnoreCase))
                {
                    IsCustomType = true;
                }
                // "text" -> panels remain collapsed by XAML triggers
            }

            private static int Snap(int value, int divisions)
            {
                var step = Math.Max(1, 255 / Math.Max(1, divisions));
                var snapped = (int)Math.Round((double)value / step) * step;
                return Math.Max(0, Math.Min(255, snapped));
            }
        }

    }
}
