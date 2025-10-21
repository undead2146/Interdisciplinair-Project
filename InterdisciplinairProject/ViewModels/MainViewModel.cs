using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace InterdiscplinairProject.ViewModels;

/// <summary>
/// Main ViewModel for the InterdisciplinairProject application.
/// <remarks>
/// This ViewModel manages the state and commands for the main window, serving as the entry point for MVVM pattern.
/// It inherits from <see cref="ObservableObject" /> to enable property change notifications.
/// Properties and commands here can bind to UI elements in <see cref="MainWindow" />.
/// Future extensions will include navigation to feature ViewModels (e.g., FixtureViewModel from Features).
/// </remarks>
/// <seealso cref="ObservableObject" />
/// <seealso cref="MainWindow" />
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "InterdisciplinairProject - DMX Lighting Control";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
    {
        Debug.WriteLine("[DEBUG] MainViewModel constructor called");

        // Initialize ViewModel, e.g., load services from DI if injected
        OpenFixtureSettingsCommand = new RelayCommand(OpenFixtureSettings);
        Debug.WriteLine("[DEBUG] MainViewModel initialized with OpenFixtureSettingsCommand");
    }

        public RelayCommand OpenFixturesCommand { get; }
        public RelayCommand OpenFixtureSettingsCommand { get; }
        public RelayCommand OpenSceneCommand { get; }

    /// <summary>
    /// Opens the fixture settings view window.
    /// </summary>
    private void OpenFixtureSettings()
    {
        Debug.WriteLine("[DEBUG] OpenFixtureSettings() called - Fixture Settings button clicked");
        var fixtureSettingsView = new InterdisciplinairProject.Views.FixtureSettingsView();
        Debug.WriteLine("[DEBUG] FixtureSettingsView instance created");
        fixtureSettingsView.Show();
        Debug.WriteLine("[DEBUG] FixtureSettingsView.Show() called - window should be visible now");
    }
}