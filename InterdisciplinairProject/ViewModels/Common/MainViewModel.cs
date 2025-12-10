using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Repositories;
using InterdisciplinairProject.Features.Scene;
using InterdisciplinairProject.Fixtures.ViewModels;
using InterdisciplinairProject.Fixtures.Views;
using InterdisciplinairProject.Services;
using InterdisciplinairProject.Views;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace InterdisciplinairProject.ViewModels;

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
    private readonly ShowbuilderViewModel _showbuilderViewModel;
    private readonly MainWindowFixturesViewModel _mainWindowFixturesViewModel;
    private readonly ISceneRepository _sceneRepository = null!;
    private readonly IFixtureRepository _fixtureRepository = null!;
    private readonly IHardwareConnection _hardwareConnection = null!;

    private readonly IFixtureRegistry _fixtureRegistry = null!;

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
                "Fixtures");
            Debug.WriteLine($"[DEBUG] Fixtures path: {fixturesPath}");
            _fixtureRepository = new FixtureRepository(fixturesPath);
            Debug.WriteLine("[DEBUG] FixtureRepository initialized");

            // Initialize HardwareConnection with real DMX service
            _hardwareConnection = new HardwareConnection();
            Debug.WriteLine("[DEBUG] HardwareConnection initialized with DMX service");

            _fixtureRegistry = new FixtureRegistry(_fixtureRepository);

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

        // Initialize ViewModel, e.g., load services from DI if injected
        _showbuilderViewModel = new ShowbuilderViewModel();
        _mainWindowFixturesViewModel = new MainWindowFixturesViewModel();
        Debug.WriteLine("[DEBUG] MainViewModel initialized with OpenFixtureSettingsCommand");

        // Show welcome view by default
        CurrentView = new WelcomeView { DataContext = this };
        Debug.WriteLine("[DEBUG] WelcomeView set as default view");
    }

    /// <summary>
    /// Gets the command to open the fixture settings view.
    /// </summary>
    public RelayCommand OpenFixtureSettingsCommand { get; private set; } = null!;

    /// <summary>
    /// Saves and closes the show.
    /// </summary>
    public void SaveCloseForShow()
    {
        _showbuilderViewModel.SaveCommand.Execute(CurrentView);
    }

    /// <summary>
    /// Opens the fixture settings view window.
    /// </summary>
    private void OpenFixtureSettings()
    {
        Debug.WriteLine("[DEBUG] OpenFixtureSettings() called");
        var fixtureSettingsView = new InterdisciplinairProject.Views.FixtureSettingsView();
        Debug.WriteLine("[DEBUG] FixtureSettingsView instance created");
    }

    [RelayCommand]
    private void OpenFixtureBuilder()
    {
        CurrentView = new MainWindowFixtures(_mainWindowFixturesViewModel);
        Title = "InterdisciplinairProject - Fixture Builder";
    }

    /// <summary>
    /// Opens the show builder view.
    /// </summary>
    [RelayCommand]
    private void OpenShowBuilder()
    {
        CurrentView = new ShowbuilderView(_showbuilderViewModel);
        Title = "InterdisciplinairProject - Showbuilder";
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
                _fixtureRegistry,
                _hardwareConnection);

            // Maak de view en geef de ViewModel mee
            var sceneBuilderView = new ScenebuilderView
            {
                DataContext = sceneBuilderViewModel,
            };

            // Toon de SceneBuilderView in het MainWindow
            // SceneBuilderViewModel handelt zijn eigen interne navigatie af naar SceneEditorView
            CurrentView = sceneBuilderView;
            Title = "InterdisciplinairProject - Scene Builder";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] OpenSceneBuilder failed: {ex.Message}");
            MessageBox.Show($"Error opening Scene Builder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}