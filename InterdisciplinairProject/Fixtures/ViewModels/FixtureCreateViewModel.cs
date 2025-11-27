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

        //partial void OnSliderDivisionsChanged(int value) => OnPropertyChanged(nameof(TickFrequency));

        /// <summary>
        /// Separator.
        /// </summary>
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

                ImageBase64 = ImageCompressionHelpers.DecompressBase64(existing.ImageBase64 ?? string.Empty);
            }
            else
            {
                _isEditing = false;
                SelectedManufacturer = AvailableManufacturers.FirstOrDefault();
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

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Manufacturer can't be empty.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                Value = "0",
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

        private void AddImage(Fixture? fixture)
        {
            if (fixture == null) return;
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

            private bool _isNameManuallyEdited = false;

            private string _name = "Lamp";

            /// <summary>
            /// Gets or sets the name of the channel.
            /// </summary>
            public string Name
            {
                get => _name;
                set
                {
                    if (SetProperty(ref _name, value))
                    {
                        _model.Name = value;

                        // mark as manually edited if it's different from the type
                        _isNameManuallyEdited = value != _selectedType;
                    }
                }
            }

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

                        // update Name only if the user hasn't manually edited it
                        if (!_isNameManuallyEdited)
                            Name = value;
                    }
                }
            }


            private int _maxValue = 255;
            public int MaxValue
            {
                get => _maxValue;
                set
                {
                    if (SetProperty(ref _maxValue, value))
                    {
                        OnPropertyChanged(nameof(TickFrequency));
                        if (Level > _maxValue)
                            Level = _maxValue;
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
                    var snapped = Snap(value, Math.Max(1, SliderDivisions), MaxValue);
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

            private bool _isDegreeHType;
            public bool IsDegreeHType
            {
                get => _isDegreeHType;
                set => SetProperty(ref _isDegreeHType, value);
            }
            private bool _isDegreeFType;
            public bool IsDegreeFType
            {
                get => _isDegreeFType;
                set => SetProperty(ref _isDegreeFType, value);
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

            public int TickFrequency => Math.Max(1, MaxValue / Math.Max(1, SliderDivisions));


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
                var name = (CustomTypeName ?? string.Empty).Trim();
                var divisions = CustomTypeSliderValue;

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Type name is empty.");
                    return;
                }
                if (divisions <= 0 || divisions > 255)
                {
                    MessageBox.Show("Step value must be between 1 and 255.");
                    return;
                }
                if (string.Equals(name, "Custom", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Choose another name than 'Custom'.");
                    return;
                }

                var spec = new TypeSpecification { name = name, input = "slider", divisions = divisions };
                if (!TypeCatalogService.AddOrUpdate(spec))
                {
                    MessageBox.Show("Failed to save the type.");
                    return;
                }

                OnPropertyChanged(nameof(AvailableTypes)); // refresh ComboBox
                SelectedType = name;                       // becomes slider with divisions
                IsCustomType = false;                      // hide custom panel
            }

            private void ApplyTypeSpec(string? typeName)
            {
                IsSliderType = false;
                IsCustomType = false;
                IsDegreeHType = false;
                IsDegreeFType = false;

                var spec = TypeCatalogService.GetByName(typeName);
                if (spec == null) return;

                if (spec.input.Equals("slider", StringComparison.OrdinalIgnoreCase))
                {
                    IsSliderType = true;
                    MaxValue = 255;

                    SliderDivisions = spec.divisions.GetValueOrDefault(255);
                }
                if (spec.input.Equals("degreeH", StringComparison.OrdinalIgnoreCase))
                {
                    IsDegreeHType = true;
                    MaxValue = 180;

                    SliderDivisions = spec.divisions.GetValueOrDefault(180);
                }
                if (spec.input.Equals("degreeF", StringComparison.OrdinalIgnoreCase))
                {
                    IsDegreeFType = true;

                    MaxValue = 360;
                    SliderDivisions = spec.divisions.GetValueOrDefault(360);
                }
                else if (spec.input.Equals("custom", StringComparison.OrdinalIgnoreCase))
                {
                    IsCustomType = true;
                }

                // "text" -> panels remain collapsed by XAML triggers
            }

            private static int Snap(int value, int divisions, int max)
            {
                var step = Math.Max(1, max / Math.Max(1, divisions));
                var snapped = (int)Math.Round((double)value / step) * step;
                return Math.Max(0, Math.Min(max, snapped));
            }
        }
    }
}
