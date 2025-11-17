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
            Debug.WriteLine("[DEBUG] SceneEditorViewModel: AddFixture called - opening ImportFixturesView");

            // Open the ImportFixturesView window
            var importView = new ImportFixturesView();
            var viewModel = importView.ViewModel;

            // Pass the current scene to the import view model
            viewModel.CurrentScene = Scene;

            // Subscribe to the CloseRequested event to know when fixtures were actually added
            viewModel.CloseRequested += async (s, e) =>
            {
                Debug.WriteLine("[DEBUG] SceneEditorViewModel: CloseRequested event received - refreshing scene");
                await RefreshSceneFromRepository();
            };

            importView.ShowDialog();

            Debug.WriteLine("[DEBUG] SceneEditorViewModel: ImportFixturesView closed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error opening import view: {ex.Message}");
            Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            MessageBox.Show($"Error opening fixture import: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Refreshes the scene data from the repository.
    /// </summary>
    private async Task RefreshSceneFromRepository()
    {
        try
        {
            if (string.IsNullOrEmpty(Scene.Id))
            {
                Debug.WriteLine($"[WARNING] SceneEditorViewModel: Scene has no ID, cannot refresh from repository");
                return;
            }

            // Reload the scene from repository to get the updated version
            var updatedScene = await _sceneRepository.GetSceneByIdAsync(Scene.Id);

            if (updatedScene != null)
            {
                LoadScene(updatedScene);
                Debug.WriteLine($"[DEBUG] SceneEditorViewModel: Refreshed scene '{updatedScene.Name}' with {updatedScene.Fixtures?.Count ?? 0} fixtures from repository");
            }
            else
            {
                Debug.WriteLine($"[WARNING] SceneEditorViewModel: Could not reload scene '{Scene.Id}' from repository");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] SceneEditorViewModel: Error refreshing scene from repository: {ex.Message}");
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

    /// <summary>
    /// Removes a fixture from the scene.
    /// </summary>
    [RelayCommand]
    private async Task RemoveFixture(SceneFixture? fixtureToRemove)
    {
        if (fixtureToRemove == null)
        {
            Debug.WriteLine("[WARNING] RemoveFixtureCommand called without parameter.");
            return;
        }

        var result = MessageBox.Show(
            $"Weet je zeker dat je de fixture '{fixtureToRemove.Fixture.Name}' wilt verwijderen?",
            "Bevestig Verwijdering",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
        {
            return;
        }

        try
        {
            Debug.WriteLine($"[DEBUG] Removing fixture '{fixtureToRemove.Fixture.Name}' from scene '{Scene.Name}'.");

            SceneFixtures.Remove(fixtureToRemove);

            await _sceneRepository.RemoveFixtureAsync(Scene.Id, fixtureToRemove.Fixture);

            if (Scene.Fixtures != null)
            {
                Scene.Fixtures.Remove(fixtureToRemove.Fixture);
            }

            if (SelectedFixture == fixtureToRemove)
            {
                SelectedFixture = null;
            }

            Debug.WriteLine($"[DEBUG] Fixture successfully removed and scene saved.");

            MessageBox.Show(
                $"Fixture '{fixtureToRemove.Fixture.Name}' succesvol verwijderd.",
                "Succes",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error removing fixture: {ex.Message}");
            MessageBox.Show($"Fout bij verwijderen van fixture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}