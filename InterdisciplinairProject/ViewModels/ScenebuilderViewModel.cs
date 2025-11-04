using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Views;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for the Scene Builder functionality.
/// </summary>
public partial class ScenebuilderViewModel : ObservableObject
{
    private readonly ISceneRepository _sceneRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IHardwareConnection _hardwareConnection;

    [ObservableProperty]
    private Scene? _selectedScene;

    [ObservableProperty]
    private object? _currentView;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenebuilderViewModel"/> class.
    /// </summary>
    /// <param name="sceneRepository">The scene repository.</param>
    /// <param name="fixtureRepository">The fixture repository.</param>
    /// <param name="hardwareConnection">The hardware connection.</param>
    public ScenebuilderViewModel(
        ISceneRepository sceneRepository,
        IFixtureRepository fixtureRepository,
        IHardwareConnection hardwareConnection)
    {
        _sceneRepository = sceneRepository;
        _fixtureRepository = fixtureRepository;
        _hardwareConnection = hardwareConnection;
        _ = LoadScenesAsync(); // fire-and-forget; constructor can't be async
    }

    /// <summary>
    /// Gets the collection of scenes.
    /// </summary>
    public ObservableCollection<Scene> Scenes { get; } = new();

    /// <summary>
    /// Opens the scene editor for the selected scene.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task OpenSceneEditor()
    {
        if (SelectedScene == null || string.IsNullOrEmpty(SelectedScene.Id))
        {
            return;
        }

        try
        {
            var fullScene = await _sceneRepository.GetSceneByIdAsync(SelectedScene.Id);

            if (fullScene != null)
            {
                var sceneEditorViewModel = new SceneEditorViewModel(
                    _sceneRepository,
                    _fixtureRepository,
                    _hardwareConnection);

                sceneEditorViewModel.LoadScene(fullScene);

                // Toon SceneEditorView IN de SceneBuilder
                CurrentView = new SceneEditorView
                {
                    DataContext = sceneEditorViewModel,
                };
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading scene: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Creates a new scene.
    /// </summary>
    [RelayCommand]
    private async Task NewScene()
    {
        try
        {
            var dlg = new InterdisciplinairProject.Views.InputDialog("Nieuwe scène", "Geef een naam voor de scène:");
            if (dlg.ShowDialog() == true)
            {
                var name = dlg.InputText?.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Naam mag niet leeg zijn.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var scene = new Scene
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Dimmer = 0,
                    Fixtures = new System.Collections.Generic.List<Fixture>(),
                };

                // Gebruik repository om op te slaan
                await _sceneRepository.SaveSceneAsync(scene);

                Scenes.Add(scene);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Deletes a scene.
    /// </summary>
    /// <param name="scene">The scene to delete.</param>
    [RelayCommand]
    private async Task DeleteScene(Scene scene)
    {
        try
        {
            if (scene == null || string.IsNullOrEmpty(scene.Id))
            {
                return;
            }

            var result = MessageBox.Show(
             $"Weet je zeker dat je scene '{scene.Name}' wilt verwijderen?",
             "Scene verwijderen",
             MessageBoxButton.YesNo,
             MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _sceneRepository.DeleteSceneAsync(scene.Id);
                Scenes.Remove(scene);

                // Als de verwijderde scene geselecteerd was, clear de view
                if (SelectedScene == scene)
                {
                    SelectedScene = null;
                    CurrentView = null;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij verwijderen van scene: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadScenesAsync()
    {
        try
        {
            var scenes = await _sceneRepository.GetAllScenesAsync();

            Scenes.Clear();
            foreach (var scene in scenes)
            {
                Scenes.Add(scene);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading scenes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
