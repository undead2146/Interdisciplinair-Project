using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using InterdisciplinairProject.Fixtures.Views;
using InterdisciplinairProject.Fixtures.Services;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models;
using System.Collections.ObjectModel;
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
        private string fixtureName = "New Fixture";

        [ObservableProperty]
        private List<string> _availableManufacturers = new();

        [ObservableProperty]
        private string? _selectedManufacturer;

        public event EventHandler? BackRequested;
        public event EventHandler? FixtureSaved;
        public ObservableCollection<ChannelViewModel> Channels { get; } = new();

        public ICommand AddChannelCommand { get; }
        public ICommand DeleteChannelCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RegisterManufacturerCommand { get; }
        public ICommand AddImageCommand { get; }

        private InterdisciplinairProject.Fixtures.Models.Fixture _currentFixture = new InterdisciplinairProject.Fixtures.Models.Fixture();

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
            DeleteChannelCommand = new RelayCommand<ChannelViewModel>(DeleteChannel, CanDeleteChannel);
            SaveCommand = new RelayCommand(SaveFixture);
            CancelCommand = new RelayCommand(Cancel);
            RegisterManufacturerCommand = new RelayCommand(ExecuteRegisterManufacturer);
            AddImageCommand = new RelayCommand<InterdisciplinairProject.Fixtures.Models.Fixture>(AddImage);

            if (existing != null)
            {
                _isEditing = true;
                FixtureName = existing.Name ?? string.Empty;
                SelectedManufacturer = existing.Manufacturer ?? "Unknown";
                _originalManufacturer = existing.Manufacturer ?? "Unknown";
                _originalFixtureName = existing.Name ?? string.Empty;

                Channels.Clear();
                foreach (var ch in existing.Channels)
                    Channels.Add(new ChannelViewModel(ch));

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
            if (string.IsNullOrEmpty(FixtureName) || Channels.Any(ch => string.IsNullOrWhiteSpace(ch.Name) || ch.SelectedType == ChannelType.Unknown))
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
                    ["Type"] = ChannelTypeHelper.GetDisplayName(ch.SelectedType),
                    ["value"] = ch.Parameter,
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
                Type = ChannelTypeHelper.GetDisplayName(ChannelType.Dimmer),
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

        private void AddImage(InterdisciplinairProject.Fixtures.Models.Fixture fixture)
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
    }
}
