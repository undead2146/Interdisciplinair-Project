using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes; // Nodig voor Code 1
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Nodig voor Code 2 KeyEventArgs
using System.Collections.ObjectModel; // Nodig voor Code 2 ObservableCollection
using InterdisciplinairProject.Fixtures.ViewModels;
using System.Windows.Media; // Nodig voor ChannelViewModel (uit Code 2)

namespace InterdisciplinairProject.Fixtures.Views
{
    // HET MODEL: De 'Channel' class uit Code 2, verplaatst naar hier (of idealiter naar een apart Models-bestand)
    public class Channel
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    /// <summary>
    /// Interaction logic for ChannelListView.xaml
    /// Dit is de samengevoegde klasse.
    /// </summary>
    public partial class ChannelListView : Window
    {
        // --- Constanten en Properties uit Code 2 ---
        private const string DataFilePath = "channels_data.json"; // Pad voor het opslaan van de kanalen
        public ObservableCollection<ChannelViewModel> Channels { get; set; }
        public ChannelViewModel? SelectedChannel { get; set; }

        public static List<string> ChannelTypes { get; } = new List<string>
        {
            "dim", "kleur", "pan", "tilt", "strobe", "iris", "focus"
        };
        // ---------------------------------------------

        public ChannelListView()
        {
            InitializeComponent();

            // --- Initialisatie logica uit Code 2 ---
            var loadedChannels = LoadChannelModels();

            // Zet de geladen modellen om in ViewModels
            Channels = new ObservableCollection<ChannelViewModel>(
                loadedChannels.Select(c => new ChannelViewModel(c))
            );

            this.DataContext = this;
            this.Closed += ChannelListView_Closed; // Naam aangepast van MainWindow_Closed
        }

        // --- Event Handlers uit Code 1 (Aangepast indien nodig) ---

        private void ChannelListView_Closed(object? sender, System.EventArgs e)
        {
            SaveChannels(); // Roep de opslagmethode van de kanalen aan bij sluiten
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string name = FixtureNameTextBox.Text;

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please fill in a valid name");
                return;
            }

            // File path logica voor het opslaan van de Fixture metadata (uit Code 1)
            string dataDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            // Zorg ervoor dat de data directory bestaat voordat we erin schrijven
            Directory.CreateDirectory(dataDir);

            string safeName = string.Concat(name.Split(System.IO.Path.GetInvalidFileNameChars()));
            string filePath = System.IO.Path.Combine(dataDir, safeName + ".json");

            //checken of bestand al bestaat
            if (File.Exists(filePath))
            {
                MessageBox.Show("There already exists a fixture with this name");
                return;
            }

            // --- Combinatie: Voeg de kanalen van Code 2 toe aan de Fixture JSON van Code 1 ---
            var channelsToSave = Channels
                                .Select(vm => new Channel { Name = vm.Name, Type = vm.Type })
                                .ToList();

            // Root JSON-object (uit Code 1)
            var root = new JsonObject
            {
                ["name"] = FixtureNameTextBox.Text ?? string.Empty,
                ["channels"] = JsonNode.Parse(JsonSerializer.Serialize(channelsToSave)) // Voeg de lijst met kanalen toe
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = root.ToJsonString(options);

            try
            {
                File.WriteAllText(filePath, json);
                // Roep ook SaveChannels aan om de "channels_data.json" bij te werken (optioneel, maar veilig)
                SaveChannels();

                MessageBox.Show($"Fixture is saved succesfully");
                this.Close();
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Error with saving fixture: {ioEx.Message}");
                this.Close();
            }
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
            messageBoxText: "Are you sure you want to cancel making this fixture?",
            caption: "Confirm Cancel",
            button: MessageBoxButton.YesNo,
            icon: MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
            // Anders: do nothing
        }

        // --- Hulpmethoden en Event Handlers uit Code 2 (MainWindow) ---

        // De oorspronkelijke SaveChannels die "channels_data.json" opslaat
        private void SaveChannels()
        {
            try
            {
                var channelsToSave = Channels
                                           .Select(vm => new Channel { Name = vm.Name, Type = vm.Type })
                                           .ToList();

                JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(channelsToSave, options);
                File.WriteAllText(DataFilePath, jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij het opslaan van data: {ex.Message}", "Opslagfout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Hulpmethode om de initiële kanalen te laden
        private ObservableCollection<Channel> LoadChannelModels()
        {
            if (File.Exists(DataFilePath))
            {
                try
                {
                    string jsonString = File.ReadAllText(DataFilePath);
                    var loadedChannels = JsonSerializer.Deserialize<List<Channel>>(jsonString);
                    if (loadedChannels != null)
                    {
                        return new ObservableCollection<Channel>(loadedChannels);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij het laden van data: {ex.Message}", "Laadfout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Standaard kanalen als het bestand niet bestaat of het laden mislukt
            return new ObservableCollection<Channel>
            {
                new Channel { Name = "Intensiteit", Type = "dim"},
                new Channel { Name = "Positie X", Type = "pan"},
                new Channel { Name = "Positie Y", Type = "tilt"}
            };
        }

        private void AddChannelButton_Click(object sender, RoutedEventArgs e)
        {
            // Maak een nieuw Model
            var newModel = new Channel { Name = $"Nieuw Kanaal {Channels.Count + 1}", Type = "kleur" };

            // Maak een nieuwe ViewModel en voeg toe
            var newChannel = new ChannelViewModel(newModel);
            Channels.Add(newChannel);

            SaveChannels();

            MessageBox.Show($"Nieuw kanaal toegevoegd: Kanaal {Channels.Count}.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChannelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ChannelViewModel newSelection)
            {
                // Stop de bewerking van het vorige kanaal
                if (SelectedChannel != null && SelectedChannel != newSelection)
                {
                    SelectedChannel.IsEditing = false;
                }

                // Start de bewerking van het geselecteerde kanaal
                newSelection.IsEditing = true;
                SelectedChannel = newSelection;
            }
        }

        private void EditTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Stop de bewerking in het ViewModel
                if (((System.Windows.FrameworkElement)sender).DataContext is ChannelViewModel channelVm)
                {
                    channelVm.IsEditing = false;

                    // Hef selectie op in de ListBox (ChannelsListBox is de naam die je in de XAML moet toekennen)
                    if (sender is ListBox listBox && listBox.SelectedItem != null)
                    {
                        listBox.SelectedItem = null;
                    }
                    // Of als het element dat de gebeurtenis activeert een TextBox is binnen een ListBox Item
                    else if (sender is TextBox textBox && textBox.DataContext is ChannelViewModel vm)
                    {
                        // Probeer de ouder ListBox te vinden om de selectie op te heffen
                        var listbox = FindParent<ListBox>(textBox);
                        if (listbox != null)
                        {
                            listbox.SelectedItem = null;
                        }
                    }
                }

                SaveChannels();
                e.Handled = true;
            }
        }

        // Hulpfunctie om de ouder ListBox te vinden voor EditTextBox_KeyDown (om selectie op te heffen)
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            T? parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                return FindParent<T>(parentObject);
            }
        }
    }
}