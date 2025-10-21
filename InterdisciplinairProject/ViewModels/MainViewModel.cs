using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace InterdisciplinairProject.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "InterdisciplinairProject - DMX Lighting Control";

    [ObservableProperty]
    private UserControl currentView;

    public MainViewModel()
    {
        Debug.WriteLine("[DEBUG] MainViewModel constructor called");

        // Initialize ViewModel, e.g., load services from DI if injected
        OpenFixtureSettingsCommand = new RelayCommand(OpenFixtureSettings);
        Debug.WriteLine("[DEBUG] MainViewModel initialized with OpenFixtureSettingsCommand");
    }

    public RelayCommand OpenFixtureSettingsCommand { get; private set; }

    private void OpenFixtureSettings()
    {
        Debug.WriteLine("[DEBUG] OpenFixtureSettings() called - Fixture Settings button clicked");
        var fixtureSettingsView = new InterdisciplinairProject.Views.FixtureSettingsView();
        Debug.WriteLine("[DEBUG] FixtureSettingsView instance created");
        fixtureSettingsView.Show();
        Debug.WriteLine("[DEBUG] FixtureSettingsView.Show() called - window should be visible now");
    }

    [RelayCommand]
    private void OpenShowBuilder()
    {
        CurrentView = new ShowbuilderView();
    }

    [RelayCommand]
    private void OpenSceneBuilder()
    {
        CurrentView = new ScenebuilderView();
    }
}
