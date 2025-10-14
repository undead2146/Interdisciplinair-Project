using InterdisciplinairProject.Fixtures.Models;
using InterdisciplinairProject.Fixtures.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Nodig voor Code 2 ObservableCollection
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes; // Nodig voor Code 1
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Nodig voor Code 2 KeyEventArgs
using System.Windows.Media; // Nodig voor ChannelViewModel (uit Code 2)

namespace InterdisciplinairProject.Fixtures.Views
{
    public partial class ChannelListView : Window
    {
        // --- Constanten en Properties ---
        private const string DataFilePath = "channels_data.json"; // Pad voor het opslaan van de kanalen
        public ObservableCollection<ChannelViewModel> Channels { get; set; }
        public ChannelViewModel? SelectedChannel { get; set; }


        public ChannelListView()
        {
            InitializeComponent();
            
            // --- Initialisatie logica uit Code 2 ---
            //var loadedChannels = LoadChannelModels();

            // Zet de geladen modellen om in ViewModels
            Channels = new ObservableCollection<ChannelViewModel>();
            
            this.DataContext = this;
        }



        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string name = FixtureNameTextBox.Text;

            // files
            string dataDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            string safeName = string.Concat(name.Split(System.IO.Path.GetInvalidFileNameChars()));
            string filePath = System.IO.Path.Combine(dataDir, safeName + ".json");


            // fout checks
            if (File.Exists(filePath))
            {
                MessageBox.Show("There already exists a fixture with this name");
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please fill in a valid name");
                return;
            }

            // channels fout cgheck
            foreach (ChannelViewModel channelVm in ChannelsListBox.Items) 
            {
                if (string.IsNullOrWhiteSpace(channelVm.Name))
                {
                    MessageBox.Show("Each channel must have a name.",
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
                    ["Type"] = ch.SelectedType
                };
                channelsArray.Add(channelObj);
            }

            var root = new JsonObject
            {
                ["name"] = name,
                ["channels"] = channelsArray
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = root.ToJsonString(options);

            try
            {
                File.WriteAllText(filePath, json);
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
            else
            {
                //do nothing
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
            var newModel = new Channel { Name = $"Nieuw Kanaal {Channels.Count + 1}", Type = string.Empty };

            // Maak een nieuwe ViewModel en voeg toe
            var newChannel = new ChannelViewModel(newModel);
            Channels.Add(newChannel);
        }
        private void ChannelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ChannelViewModel newSelection)
            {
                // Collapse previous channel
                if (SelectedChannel != null && SelectedChannel != newSelection)
                {
                    SelectedChannel.IsExpanded = false;
                    SelectedChannel.IsEditing = false;
                }

                // Toggle expansion for the new selection
                newSelection.IsExpanded = !newSelection.IsExpanded;
                SelectedChannel = newSelection;
            }
        }

        private void ChannelsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChannelsListBox.SelectedItem is ChannelViewModel selected)
            {
                selected.IsEditing = true;
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