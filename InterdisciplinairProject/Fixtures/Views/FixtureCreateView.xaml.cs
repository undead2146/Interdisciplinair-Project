using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using InterdisciplinairProject.Fixtures.ViewModels;
using static InterdisciplinairProject.Fixtures.ViewModels.FixtureCreateViewModel;
using InterdisciplinairProject.Fixtures.Services;

namespace InterdisciplinairProject.Fixtures.Views
{
    public partial class FixtureCreateView : UserControl
    {
        private ChannelItem? _selectedChannel; // Veld om de geselecteerde staat bij te houden
        private Point _dragStartPoint;  // needed for drag drop functionality for reordering channels

        public FixtureCreateView()
        {
            InitializeComponent();
        }

        private void ChannelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ChannelItem newSelection)
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
            if (ChannelsListBox.SelectedItem is ChannelItem selected)
            {
                selected.IsEditing = true;
            }
        }

        private void EditTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (((System.Windows.FrameworkElement)sender).DataContext is ChannelItem channelVm)
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

        // start of drag drop functionality for reordering channels
        private bool IsHeaderClicked(DependencyObject source)
        {
            while (source != null)
            {
                // voorkom drag als je op de naam klikt (dubbelklik rename mag NIET slepen)
                if (source is FrameworkElement fe && fe.Name == "ChannelName")
                    return false;

                // drag alleen toestaan binnen DragBorder
                if (source is Border b && b.Name == "DragBorder")
                    return true;

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }



        private void ChannelsListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject source = e.OriginalSource as DependencyObject;
            var listBox = (ListBox)sender;

            var item = GetNearestContainer(listBox, e.GetPosition(listBox));
            if (item != null)
            {
                listBox.SelectedItem = item.DataContext;
            }

            if (IsHeaderClicked(source))
            {
                _dragStartPoint = e.GetPosition(null);
            }
            else
            {
                _dragStartPoint = default;
            }
        }





        private void ChannelsListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragStartPoint == default || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is not ListBox listBox || listBox.SelectedItem == null)
                    return;

                DragDrop.DoDragDrop(listBox, listBox.SelectedItem, DragDropEffects.Move);
            }
        }

        private void ChannelsListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;

            if (sender is not ListBox listBox)
                return;

            ScrollViewer scrollViewer = FindScrollViewer(listBox);
            if (scrollViewer != null)
            {
                Point position = e.GetPosition(listBox);
                const double scrollMargin = 20;
                const double scrollSpeed = 3;

                if (position.Y < scrollMargin)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - scrollSpeed);
                }
                else if (position.Y > listBox.ActualHeight - scrollMargin)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + scrollSpeed);
                }
            }
        }

        private void ChannelsListBox_Drop(object sender, DragEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            var source = e.Data.GetData(typeof(ChannelItem)) as ChannelItem;
            if (source == null)
                return;

            var target = GetNearestContainer(listBox, e.GetPosition(listBox))?.DataContext as ChannelItem;
            if (target == null || ReferenceEquals(source, target))
                return;

            if (DataContext is not FixtureCreateViewModel vm)
                return;

            int oldIndex = vm.Channels.IndexOf(source);
            int newIndex = vm.Channels.IndexOf(target);

            if (oldIndex != newIndex)
                vm.Channels.Move(oldIndex, newIndex);
        }

        private ListBoxItem GetNearestContainer(ListBox listBox, Point position)
        {
            var element = listBox.InputHitTest(position) as DependencyObject;

            while (element != null && element is not ListBoxItem)
                element = VisualTreeHelper.GetParent(element);

            return element as ListBoxItem;
        }

        private ScrollViewer FindScrollViewer(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is ScrollViewer viewer)
                    return viewer;

                var result = FindScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }

    // end of drag drop functionality for reordering channels
        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            // allow only digits
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void BlockSpace(object sender, KeyEventArgs e)
        {
            // prevent spacebar from entering " " which breaks integer parsing
            if (e.Key == Key.Space)
                e.Handled = true;
        }
    }
}