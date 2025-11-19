using InterdisciplinairProject.Fixtures.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;

namespace InterdisciplinairProject.Fixtures.Views
{
    public partial class FixtureCreateView : UserControl
    {
        private ChannelViewModel? _selectedChannel; // Veld om de geselecteerde staat bij te houden

        public FixtureCreateView()
        {
            InitializeComponent();
        }

        private void ChannelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ChannelViewModel newSelection)
            {
                // Sluit het eerder geselecteerde kanaal
                if (_selectedChannel != null && _selectedChannel != newSelection)
                {
                    _selectedChannel.IsExpanded = false;
                    _selectedChannel.IsEditing = false;
                }

                // Toggle expansion voor de nieuwe selectie
                newSelection.IsExpanded = !newSelection.IsExpanded;
                _selectedChannel = newSelection;
            }

            // Reset de selectie als deze verdwijnt
            if (ChannelsListBox.SelectedItem == null)
            {
                _selectedChannel = null;
            }
        }

        private void ChannelsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Start de bewerkingsmodus bij dubbelklik
            if (ChannelsListBox.SelectedItem is ChannelViewModel selected)
            {
                selected.IsEditing = true;
            }
        }

        private void EditTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (((System.Windows.FrameworkElement)sender).DataContext is ChannelViewModel channelVm)
                {
                    channelVm.IsEditing = false; // Stop de bewerking

                    // Probeer de ouder ListBox te vinden om de selectie op te heffen
                    if (sender is TextBox textBox)
                    {
                        var listbox = FindParent<ListBox>(textBox);
                        if (listbox != null)
                        {
                            listbox.SelectedItem = null; // Heft selectie op
                        }
                    }
                }
                e.Handled = true; // Voorkom dat Enter andere knoppen activeert
            }
        }

        // Hulpfunctie om de ouder ListBox te vinden
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