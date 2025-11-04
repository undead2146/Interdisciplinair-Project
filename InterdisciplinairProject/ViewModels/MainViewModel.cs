using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Repositories;
using InterdisciplinairProject.Views;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// Main ViewModel for the InterdisciplinairProject application.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISceneRepository _sceneRepository = null!;
    private readonly IFixtureRepository _fixtureRepository = null!;
    private readonly IHardwareConnection _hardwareConnection = null!;

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    [ObservableProperty]
    private string title = "InterdisciplinairProject - DMX Lighting Control";

    /// <summary>
    /// Gets or sets the current view displayed in the main window.
    /// </summary>
    [ObservableProperty]
    private UserControl? currentView;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
    {
        Debug.WriteLine("[DEBUG] MainViewModel constructor called");

        try
        {
            // Initialiseer SceneRepository
            var scenesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "scenes.json");
            Debug.WriteLine($"[DEBUG] Scenes path: {scenesPath}");
            _sceneRepository = new SceneRepository(scenesPath);
            Debug.WriteLine("[DEBUG] SceneRepository initialized");

            // Initialiseer FixtureRepository
            var fixturesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject",
                "fixtures.json");
            Debug.WriteLine($"[DEBUG] Fixtures path: {fixturesPath}");
            _fixtureRepository = new FixtureRepository(fixturesPath);
            Debug.WriteLine("[DEBUG] FixtureRepository initialized");

            // Initialiseer HardwareConnection (dummy voor nu)
            _hardwareConnection = new DummyHardwareConnection();
            Debug.WriteLine("[DEBUG] DummyHardwareConnection initialized");

            OpenFixtureSettingsCommand = new RelayCommand(OpenFixtureSettings);
            Debug.WriteLine("[DEBUG] MainViewModel initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] MainViewModel initialization failed: {ex.Message}");
            Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            MessageBox.Show(
                $"Error initializing application: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Gets the command to open the fixture settings view.
    /// </summary>
    public RelayCommand OpenFixtureSettingsCommand { get; private set; } = null!;

    /// <summary>
    /// Opens the fixture settings view window.
    /// </summary>
    private void OpenFixtureSettings()
    {
        Debug.WriteLine("[DEBUG] OpenFixtureSettings() called");
        var fixtureSettingsView = new InterdisciplinairProject.Views.FixtureSettingsView();
        Debug.WriteLine("[DEBUG] FixtureSettingsView instance created");
    }

    /// <summary>
    /// Opens the show builder view.
    /// </summary>
    [RelayCommand]
    private void OpenShowBuilder()
    {
        CurrentView = new ShowbuilderView();
    }

    /// <summary>
    /// Opens the scene builder view.
    /// </summary>
    [RelayCommand]
    private void OpenSceneBuilder()
    {
        try
        {
            // Maak de ViewModel aan met alle benodigde repositories
            var sceneBuilderViewModel = new ScenebuilderViewModel(
                _sceneRepository,
                _fixtureRepository,
                _hardwareConnection);

            // Maak de view en geef de ViewModel mee
            var sceneBuilderView = new ScenebuilderView
            {
                DataContext = sceneBuilderViewModel,
            };

            // Toon de SceneBuilderView in het MainWindow
            // SceneBuilderViewModel handelt zijn eigen interne navigatie af naar SceneEditorView
            CurrentView = sceneBuilderView;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] OpenSceneBuilder failed: {ex.Message}");
            MessageBox.Show($"Error opening Scene Builder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
