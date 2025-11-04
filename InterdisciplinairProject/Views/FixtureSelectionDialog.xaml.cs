using System.Collections.Generic;
using System.Windows;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Views;

/// <summary>
/// Interaction logic for FixtureSelectionDialog.xaml.
/// </summary>
public partial class FixtureSelectionDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureSelectionDialog"/> class.
    /// </summary>
    /// <param name="availableFixtures">The list of available fixtures to display.</param>
    public FixtureSelectionDialog(List<Fixture> availableFixtures)
    {
        InitializeComponent();
        FixtureListBox.ItemsSource = availableFixtures;
        
        if (availableFixtures.Count > 0)
        {
            FixtureListBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Gets the selected fixture.
    /// </summary>
    public Fixture? SelectedFixture => FixtureListBox.SelectedItem as Fixture;

    /// <summary>
    /// Handles the OK button click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedFixture == null)
        {
            MessageBox.Show("Selecteer eerst een fixture.", "Geen selectie", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Handles the Cancel button click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
