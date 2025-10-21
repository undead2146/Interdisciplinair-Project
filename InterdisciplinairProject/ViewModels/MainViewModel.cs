using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows;

namespace InterdiscplinairProject.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        Debug.WriteLine("[DEBUG] MainViewModel constructor called");
        Console.WriteLine("[DEBUG] MainViewModel constructor called");

        // Initialize ViewModel, e.g., load services from DI if injected
        OpenFixtureSettingsCommand = new RelayCommand(OpenFixtureSettings);
        Debug.WriteLine("[DEBUG] MainViewModel initialized with OpenFixtureSettingsCommand");
        Console.WriteLine("[DEBUG] MainViewModel initialized with OpenFixtureSettingsCommand");
    }

    /// <summary>
    /// Gets the command to open the fixture settings view.
    /// </summary>
    public RelayCommand OpenFixtureSettingsCommand { get; private set; }

    /// <summary>
    /// Opens the fixture settings view window.
    /// </summary>
    private void OpenFixtureSettings()
    {
        Debug.WriteLine("[DEBUG] OpenFixtureSettings() called - Fixture Settings button clicked");
        Console.WriteLine("[DEBUG] OpenFixtureSettings() called - Fixture Settings button clicked");
        var fixtureSettingsView = new InterdisciplinairProject.Views.FixtureSettingsView();
        Debug.WriteLine("[DEBUG] FixtureSettingsView instance created");
        Console.WriteLine("[DEBUG] FixtureSettingsView instance created");
        fixtureSettingsView.Show();
        Debug.WriteLine("[DEBUG] FixtureSettingsView.Show() called - window should be visible now");
        Console.WriteLine("[DEBUG] FixtureSettingsView.Show() called - window should be visible now");
    }
}
