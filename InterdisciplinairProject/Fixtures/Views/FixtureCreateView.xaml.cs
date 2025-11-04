using InterdisciplinairProject.Fixtures.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace InterdisciplinairProject.Fixtures.Views
{
    /// <summary>
    /// Interaction logic for FixtureCreateView.xaml
    /// </summary>
    public partial class FixtureCreateView : UserControl
    {
        // --- Constanten en Properties ---
        private const string DataFilePath = "channels_data.json"; // Pad voor het opslaan van de kanalen
        public ObservableCollection<ChannelViewModel> Channels { get; set; }
        public ChannelViewModel? SelectedChannel { get; set; }


        public FixtureCreateView()
        {
            InitializeComponent();

            // Zet de geladen modellen om in ViewModels
            Channels = new ObservableCollection<ChannelViewModel>();
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
