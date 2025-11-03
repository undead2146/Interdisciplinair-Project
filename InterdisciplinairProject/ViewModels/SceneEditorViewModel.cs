using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for editing a scene.
/// </summary>
public partial class SceneEditorViewModel : ObservableObject
{
    private readonly ISceneRepository _sceneRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IHardwareConnection _hardwareConnection;

    [ObservableProperty]
    private Scene _scene = new();

    [ObservableProperty]
    private ObservableCollection<SceneFixture> _sceneFixtures = new();

    [ObservableProperty]
    private SceneFixture? _selectedFixture;

    [ObservableProperty]
    private object? _currentView;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneEditorViewModel"/> class.
    /// </summary>
    /// <param name="sceneRepository">The scene repository.</param>
    /// <param name="fixtureRepository">The fixture repository.</param>
    /// <param name="hardwareConnection">The hardware connection.</param>
    public SceneEditorViewModel(ISceneRepository sceneRepository, IFixtureRepository fixtureRepository, IHardwareConnection hardwareConnection)
    {
        _sceneRepository = sceneRepository;
        _fixtureRepository = fixtureRepository;
        _hardwareConnection = hardwareConnection;
    }

    /// <summary>
    /// Loads a scene for editing.
    /// </summary>
    /// <param name="scene">The scene to load.</param>
    public void LoadScene(Scene scene)
    {
        if (scene == null)
        {
            return;
        }

        Scene = scene;
        SceneFixtures.Clear();

        var currentChannel = 1;
        if (scene.Fixtures != null)
        {
            foreach (var fixture in scene.Fixtures)
            {
                SceneFixtures.Add(new SceneFixture { Fixture = fixture, StartChannel = currentChannel });
                currentChannel += fixture.Channels.Count;
            }
        }
    }

    /// <summary>
    /// Saves the scene.
    /// </summary>
    [RelayCommand]
    private async Task SaveScene()
    {
        try
        {
            Debug.WriteLine($"[DEBUG] Saving scene '{Scene.Name}' with {Scene.Fixtures?.Count ?? 0} fixtures");

            // Sla de complete scene op via repository
            await _sceneRepository.SaveSceneAsync(Scene);

            MessageBox.Show("Scene succesvol opgeslagen!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            Debug.WriteLine($"[DEBUG] Scene '{Scene.Name}' saved successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error saving scene: {ex.Message}");
            MessageBox.Show($"Error saving scene: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Adds a new fixture to the scene.
    /// </summary>
    [RelayCommand]
    private async Task AddFixture()
    {
        try
        {
            // Get all available fixtures from repository
            var availableFixtures = await _fixtureRepository.GetAllFixturesAsync();

            if (availableFixtures == null || availableFixtures.Count == 0)
            {
                MessageBox.Show("Geen fixtures beschikbaar. Voeg eerst fixtures toe aan de fixture library.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // For now, just add the first available fixture
            // TODO: Show a dialog to let user select which fixture to add
            var fixtureToAdd = availableFixtures[0];

            // Create a copy of the fixture with a unique instance ID
            var newFixture = new Fixture
            {
                Id = fixtureToAdd.Id,
                InstanceId = Guid.NewGuid().ToString(),
                Name = fixtureToAdd.Name,
                Channels = new Dictionary<string, byte?>(fixtureToAdd.Channels),
            };

            // Add to scene
            if (Scene.Fixtures == null)
            {
                Scene.Fixtures = new List<Fixture>();
            }

            Scene.Fixtures.Add(newFixture);

            // Add to UI collection
            var sceneFixture = new SceneFixture
            {
                Fixture = newFixture,
                StartChannel = GetNextAvailableChannel(),
            };

            SceneFixtures.Add(sceneFixture);

            Debug.WriteLine($"[DEBUG] Added fixture '{newFixture.Name}' to scene");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error adding fixture: {ex.Message}");
            MessageBox.Show($"Error adding fixture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens the fixture settings view when a fixture is selected.
    /// </summary>
    partial void OnSelectedFixtureChanged(SceneFixture? value)
    {
        if (value?.Fixture != null)
        {
            // Maak een nieuwe FixtureSettingsViewModel
            var fixtureSettingsViewModel = new FixtureSettingsViewModel(_hardwareConnection);
            fixtureSettingsViewModel.LoadFixture(value.Fixture);

            // Laad de FixtureSettingsView
            CurrentView = new FixtureSettingsView
            {
                DataContext = fixtureSettingsViewModel
            };
        }
        else
        {
            CurrentView = null;
        }
    }

    private int GetNextAvailableChannel()
    {
        // Simple logic: find the highest end channel and add 1
        var maxChannel = 0;
        foreach (var sf in SceneFixtures)
        {
            var endChannel = sf.StartChannel + sf.Fixture.Channels.Count - 1;
            if (endChannel > maxChannel)
            {
                maxChannel = endChannel;
            }
        }

        return maxChannel + 1;
    }
}
