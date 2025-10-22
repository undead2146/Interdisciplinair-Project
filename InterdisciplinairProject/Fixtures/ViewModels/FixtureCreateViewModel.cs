using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public partial class FixtureCreateViewModel : ObservableObject
    {
        private readonly string _dataDir;
        private readonly bool _isEditing;
        private readonly string? _originalFileName;

        public event EventHandler? BackRequested;

        public event EventHandler? FixtureSaved;

        [ObservableProperty]
        private string fixtureName = string.Empty;

        public ObservableCollection<ChannelViewModel> Channels { get; } = new();

        public ICommand AddChannelCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        public FixtureCreateViewModel(FixtureContentViewModel? existing = null) 
        {
            _dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "fixtures");

            AddChannelCommand = new RelayCommand(AddChannel);
            SaveCommand = new RelayCommand(SaveFixture);
            CancelCommand = new RelayCommand(Cancel);

            // editen
            if (existing != null)
            {
                _isEditing = true;
                FixtureName = existing.Name ?? string.Empty;
                _originalFileName = Path.Combine(_dataDir, string.Concat(FixtureName.Split(Path.GetInvalidFileNameChars())) + ".json");

                Channels.Clear();
                foreach (var ch in existing.Channels)
                {
                    Channels.Add(new ChannelViewModel(ch));
                }
            }
            else
            {
                _isEditing = false;
            }
        }

        private void AddChannel()
        {
            // Maak een nieuw Model
            var newModel = new Channel { Name = $"Nieuw Kanaal {Channels.Count + 1}", Type = string.Empty };

            // Maak een nieuwe ViewModel en voeg toe
            var newChannel = new ChannelViewModel(newModel);
            Channels.Add(newChannel);
        }

        private void Cancel()
        {
            var result = MessageBox.Show(
            messageBoxText: "Are you sure you want to cancel making this fixture?",
            caption: "Confirm Cancel",
            button: MessageBoxButton.YesNo,
            icon: MessageBoxImage.Warning
);

            if (result == MessageBoxResult.Yes)
            {
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // do nothing
            }
        }

        private void SaveFixture()
        {
            Directory.CreateDirectory(_dataDir);

            // Maak een veilige bestandsnaam
            string safeName = string.Concat(FixtureName.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(_dataDir, safeName + ".json");


            // fout checks
            if (!_isEditing && File.Exists(filePath))
            {
                MessageBox.Show("There already exists a fixture with this name");
                return;
            }

            if (string.IsNullOrEmpty(FixtureName))
            {
                MessageBox.Show("Please fill in a valid name");
                return;
            }

            // channels fout cgheck
            foreach (var channelVm in Channels)
            {
                if (string.IsNullOrWhiteSpace(channelVm.Name))
                {
                    MessageBox.Show(
                        "Each channel must have a name.",
                        "Missing Channel Name", MessageBoxButton.OK);
                    return;
                }

                if (string.IsNullOrEmpty(channelVm.SelectedType))
                {
                    MessageBox.Show($"Please select a type for channel '{channelVm.Name}'.",
                        "Missing Channel Type", MessageBoxButton.OK);
                    return;
                }
            }

            // Json aanmaken
            var channelsArray = new JsonArray();

            foreach (var ch in Channels)
            {
                var channelObj = new JsonObject
                {
                    ["Name"] = ch.Name,
                    ["Type"] = ch.SelectedType,
                    ["value"] = ch.Parameter
                };
                channelsArray.Add(channelObj);
            }

            var root = new JsonObject
            {
                ["name"] = FixtureName,
                ["channels"] = channelsArray
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = root.ToJsonString(options);

            try
            {
                // Als we bewerken én de naam veranderen -> oud bestand verwijderen
                if (_isEditing && _originalFileName != null && !string.Equals(_originalFileName, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(_originalFileName))
                        File.Delete(_originalFileName);
                }

                File.WriteAllText(filePath, json);
                MessageBox.Show($"Fixture is saved succesfully");
                FixtureSaved?.Invoke(this, EventArgs.Empty);
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Error with saving fixture: {ioEx.Message}");
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
