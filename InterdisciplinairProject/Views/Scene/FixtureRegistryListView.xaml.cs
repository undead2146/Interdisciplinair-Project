using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InterdisciplinairProject.Views.Scene;

/// <summary>
/// Interaction logic for FixtureRegistryListView.xaml.
/// </summary>
public partial class FixtureRegistryListView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRegistryListView"/> class.
    /// </summary>
    public FixtureRegistryListView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the Import Fixture button click.
    /// </summary>
    private async void ImportFixtureButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FixtureRegistryListViewModel viewModel)
            return;

        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecteer fixture bestand om te importeren",
            Filter = "JSON bestanden (*.json)|*.json|Alle bestanden (*.*)|*.*",
            Multiselect = false,
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var imported = await viewModel.ImportFixturesFromFileAsync(openFileDialog.FileName);

                if (imported > 0)
                {
                    MessageBox.Show(
                        $"{imported} fixture(s) succesvol geïmporteerd!",
                        "Import succesvol",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Geen fixtures geïmporteerd.",
                        "Import",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fout bij importeren: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Handles the selection changed event (kept for potential future use).
    /// </summary>
    private void FixtureListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Optional: handle selection if needed
    }

    /// <summary>
    /// Handles double-click on a fixture in the list.
    /// </summary>
    private void FixtureListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox listBox &&
            listBox.SelectedItem is Fixture selectedFixture &&
            DataContext is FixtureRegistryListViewModel viewModel)
        {
            // Notify the ViewModel that a fixture was double-clicked
            viewModel.OnFixtureDoubleClicked(selectedFixture);
        }
    }
}